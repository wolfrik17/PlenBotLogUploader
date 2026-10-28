using Avalonia.Threading;
using Hardstuck.GuildWars2.BuildCodes.V2;
using Hardstuck.GuildWars2.MumbleLink;
using Hardstuck.Http;
using Newtonsoft.Json;
using PlenBotLogUploader.Aleeva;
using PlenBotLogUploader.AppSettings;
using PlenBotLogUploader.ArcDps;
using PlenBotLogUploader.DiscordApi;
using PlenBotLogUploader.DpsReport;
using PlenBotLogUploader.Gw2Bot;
using PlenBotLogUploader.Gw2Wingman;
using PlenBotLogUploader.RemotePing;
using PlenBotLogUploader.Teams;
using PlenBotLogUploader.Tools;
using PlenBotLogUploader.Twitch;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TwitchIrcClient;
using ZLinq;
using Timer = System.Timers.Timer;

namespace PlenBotLogUploader.Services;

internal sealed class LogUploaderService : ILogUploaderController, IDisposable
{
    private const string PlenBotVersionFileUrl = "https://raw.githubusercontent.com/wolfrik17/PlenBotLogUploader/main/VERSION";
    private const int MinFileSize = 300;
    private const string Gw2BotApiBaseUrl = "https://api.gw2bot.info/v1";

    private static readonly Dictionary<string, string> DefaultPostData = new()
    {
        { "generator", "ei" },
        { "json", "1" },
    };

    private readonly SemaphoreSlim _semaphore;
    private readonly RestClient _logPoster;
    private readonly Gw2WingmanUploader _wingmanUploader = new();
    private readonly HttpClientController _gw2BotClient = new();
    private readonly Dictionary<string, string> _twitchCommandReplacements = new();
    private readonly Dictionary<string, int> _uploadFailCounters = new();
    private readonly Timer _failedLogsTimer = new() { Interval = 150000 };
    private readonly Timer _updateCheckTimer = new() { Interval = 3600000 };

    private ArcLogsChangeObserver _watcher;
    private TwitchChatClient _twitchClient;
    private int _logsCount;
    private int _lastLogBossId = -1;
    private int _lastLogPullCounter;
    private int _twitchReconnectFailCounter;
    private bool _isDisposed;

    public static LogUploaderService Instance { get; private set; }

    public ObservableCollection<LogUploadItem> RecentUploads { get; } = [];
    public ObservableCollection<LogMessageItem> ActivityLogs { get; } = [];
    public List<DpsReportJson> SessionLogs { get; } = [];
    public List<string> AllSessionLogs { get; } = [];

    public HttpClientController HttpClientController { get; } = new();
    public MumbleReader MumbleReader { get; private set; }

    public event Action<string, string, int> NotificationRequested;
    public event Action<int> LogsCountChanged;
    public event Action<bool> MonitoringStateChanged;
    public event Action<bool, bool> SessionStateChanged; // isRunning, isPaused
    public event Action<bool> TwitchStateChanged;

    public bool IsMonitoring => _watcher?.IsRunning ?? false;
    public bool IsSessionRunning { get; private set; }
    public bool IsSessionPaused { get; private set; }
    public bool IsTwitchConnected => _twitchClient is not null;
    public int LogsCount => _logsCount;
    public string LastLogMessage { get; private set; } = "";

    public LogUploaderService()
    {
        Instance = this;
        var maxUploads = Math.Max(1, ApplicationSettings.Current.MaxConcurrentUploads);
        _semaphore = new SemaphoreSlim(maxUploads, maxUploads);

        _logPoster = new RestClient(new RestClientOptions
        {
            Timeout = TimeSpan.FromMinutes(8),
        }, configureSerialization: s => s.UseNewtonsoftJson());

        try
        {
            MumbleReader = new MumbleReader(false);
        }
        catch
        {
            MumbleReader = null;
        }

        _failedLogsTimer.Elapsed += TimerFailedLogsElapsed;
        _updateCheckTimer.Elapsed += async (_, _) => await CheckForUpdatesAsync(true);
    }

    public void Initialize()
    {
        // Load data stores
        Bosses.FromJsonFile(Bosses.JsonFileLocation);
        DiscordWebhooks.LoadDiscordWebhooks();
        Teams.Teams.LoadTeams();
        PingConfigurations.LoadPings();
        TwitchCommands.Load();
        AleevaIntegrations.Load();

        if (!File.Exists(Path.Combine(ApplicationSettings.LocalDir, "uploaded_logs.csv")))
        {
            try
            {
                File.AppendAllText(Path.Combine(ApplicationSettings.LocalDir, "uploaded_logs.csv"),
                    "Boss;BossId;Success;Duration;RecordedBy;EliteInsightsVersion;arcdpsVersion;Permalink;UserToken\n");
            }
            catch { }
        }

        // Initialize ArcLogs observer if directory is set
        if (!string.IsNullOrWhiteSpace(ApplicationSettings.Current.LogsLocation) && Directory.Exists(ApplicationSettings.Current.LogsLocation))
        {
            StartMonitoring(ApplicationSettings.Current.LogsLocation);
        }

        if (ApplicationSettings.Current.Twitch.ConnectToTwitch)
        {
            _ = ConnectTwitchBotAsync();
        }

        _updateCheckTimer.Start();
        if (ApplicationSettings.Current.AutoUpdate)
        {
            _ = CheckForUpdatesAsync(true);
        }
    }

    public void StartMonitoring(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            AddToText(">>> Cannot start monitoring: Log directory does not exist.");
            return;
        }

        ApplicationSettings.Current.LogsLocation = directory;
        ApplicationSettings.Current.Save();

        _watcher?.Dispose();
        _watcher = new ArcLogsChangeObserver(OnLogCreated);
        var mode = ApplicationSettings.Current.UsePollingForLogs
            ? ArcLogsChangeObserverMode.Polling
            : ArcLogsChangeObserverMode.FileSystemWatcher;

        _watcher.InitAndStart(directory, mode);
        AddToText($">>> Monitoring arcDPS logs at: {directory} (Mode: {mode})");
        MonitoringStateChanged?.Invoke(true);
    }

    public void StopMonitoring()
    {
        if (_watcher is not null)
        {
            _watcher.Dispose();
            _watcher = null;
            AddToText(">>> Log monitoring stopped.");
            MonitoringStateChanged?.Invoke(false);
        }
    }

    public void ChangeMonitoringMode(bool usePolling)
    {
        ApplicationSettings.Current.UsePollingForLogs = usePolling;
        ApplicationSettings.Current.Save();

        if (_watcher is not null)
        {
            var mode = usePolling ? ArcLogsChangeObserverMode.Polling : ArcLogsChangeObserverMode.FileSystemWatcher;
            _watcher.ChangeMode(mode);
            AddToText($">>> Changed log observer mode to: {mode}");
        }
    }

    private async void OnLogCreated(FileInfo file)
    {
        Interlocked.Increment(ref _logsCount);
        UpdateLogCount();

        if (!ApplicationSettings.Current.Upload.Enabled)
        {
            return;
        }

        try
        {
            if (file.Length >= MinFileSize)
            {
                var zipFileLocation = file.FullName;
                var archived = false;

                // Wait for arcDPS to finish writing
                await Task.Delay(1700);

                if (!file.FullName.EndsWith(".zevtc", StringComparison.OrdinalIgnoreCase))
                {
                    zipFileLocation = Path.Combine(ApplicationSettings.LocalDir, $"{Path.GetFileNameWithoutExtension(file.FullName)}.zevtc");
                    using var zipfile = ZipFile.Open(zipFileLocation, ZipArchiveMode.Create);
                    zipfile.CreateEntryFromFile(file.FullName, file.Name);
                    archived = true;
                }

                try
                {
                    await UploadLogFileAsync(zipFileLocation, DefaultPostData);
                }
                finally
                {
                    if (archived && File.Exists(zipFileLocation))
                    {
                        File.Delete(zipFileLocation);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Interlocked.Decrement(ref _logsCount);
            UpdateLogCount();
            AddToText($">: Unable to upload file: {file.FullName} ({ex.Message})");
        }
    }

    public async Task UploadLogFileAsync(string file, Dictionary<string, string> postData, bool bypassMessage = false)
    {
        await _semaphore.WaitAsync();
        try
        {
            await HttpUploadLogInternalAsync(file, postData, bypassMessage);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task HttpUploadLogInternalAsync(string file, Dictionary<string, string> postData, bool bypassMessage = false)
    {
        var fileName = Path.GetFileName(file);
        AddToText($">: Uploading {fileName}...");

        var request = new RestRequest(CreateDpsReportLink(), Method.Post)
        {
            RequestFormat = DataFormat.Json,
            Timeout = TimeSpan.FromMinutes(8),
        };
        request.AddBody(postData);

        try
        {
            request.AddFile("file", file);
            var responseMessage = await _logPoster.ExecuteAsync(request);

            if (!responseMessage.IsSuccessStatusCode)
            {
                var statusCode = (int)responseMessage.StatusCode;
                if (statusCode == 403 || statusCode == 408)
                {
                    if (statusCode == 403 && responseMessage.Content is not null)
                    {
                        var reportJsonErr = JsonConvert.DeserializeObject<DpsReportJson>(responseMessage.Content.Replace("\"players\": []", "\"players\": {}"));
                        AddToText($">: Unable to upload {fileName}: dps.report returned Forbidden (403): {reportJsonErr?.Error}");
                        if (reportJsonErr?.Error?.Contains("EI Failure") == true ||
                            reportJsonErr?.Error?.Contains("An identical file was uploaded recently") == true ||
                            reportJsonErr?.Error?.Contains("Encounter is too short") == true)
                        {
                            LogReuploader.RemoveLogAndSave(file);
                            return;
                        }
                    }
                    else
                    {
                        AddToText($">: Unable to upload {fileName}: dps.report returned {(statusCode == 408 ? "Timeout (408)" : "Forbidden (403)")}. Retrying shortly...");
                    }
                    await HandleQuickRetryAsync(file, postData, bypassMessage);
                    return;
                }

                if (statusCode == 429 || statusCode >= 500)
                {
                    AddToText($">: Unable to upload {fileName}: server error ({statusCode}). Added to retry queue.");
                    LogReuploader.FailedLogs.Add(file);
                    LogReuploader.SaveFailedLogs();
                    _failedLogsTimer.Start();
                    return;
                }

                AddToText($">: Unable to upload {fileName}: dps.report returned status {statusCode}.");
                return;
            }

            var content = responseMessage.Content?.Replace("\"players\": []", "\"players\": {}");
            var reportJson = JsonConvert.DeserializeObject<DpsReportJson>(content);

            if (!string.IsNullOrEmpty(reportJson?.Error))
            {
                AddToText($">: Error processing {fileName}: {reportJson.Error}");
                if (string.IsNullOrWhiteSpace(reportJson.Permalink))
                {
                    return;
                }
                AddToText(">: Log link generated despite error, processing...");
            }

            if (reportJson is null)
            {
                AddToText($">: Failed to parse response for {fileName}.");
                return;
            }

            var bossId = reportJson.Encounter.BossId;
            var success = reportJson.Encounter.Success ?? false;

            // Fetch Elite Insights Extra JSON if available
            if (reportJson.Encounter.JsonAvailable ?? false)
            {
                try
                {
                    var extraJsonUrl = $"{ApplicationSettings.Current.Upload.DpsReportServerLink}/getJson?permalink={reportJson.ConfigAwarePermalink}";
                    var jsonString = await HttpClientController.DownloadFileToStringAsync(extraJsonUrl);
                    var extraJson = JsonConvert.DeserializeObject<DpsReportJsonExtraJson>(jsonString);
                    if (extraJson?.Duration != null)
                    {
                        reportJson.ExtraJson = extraJson;
                        bossId = reportJson.ExtraJson.TriggerId;
                    }
                }
                catch
                {
                    reportJson.ExtraJson = null;
                }
            }

            // Save to uploaded_logs.csv
            if (ApplicationSettings.Current.Upload.SaveToCsvEnabled)
            {
                try
                {
                    var csvPath = Path.Combine(ApplicationSettings.LocalDir, "uploaded_logs.csv");
                    var line = $"{reportJson.ExtraJson?.FightName ?? reportJson.Encounter.Boss};{bossId};{(success ? "true" : "false")};{reportJson.ExtraJson?.Duration ?? ""};{reportJson.ExtraJson?.RecordedByAccountName ?? ""};{reportJson.ExtraJson?.EliteInsightsVersion ?? ""};{reportJson.Evtc.Type}{reportJson.Evtc.Version};{reportJson.ConfigAwarePermalink};{reportJson.UserToken}\n";
                    File.AppendAllText(csvPath, line);
                }
                catch (Exception ex)
                {
                    AddToText($">: Error writing to CSV: {ex.Message}");
                }
            }

            AllSessionLogs.Add(reportJson.ConfigAwarePermalink);
            if (IsSessionRunning && !IsSessionPaused)
            {
                SessionLogs.Add(reportJson);
            }

            LastLogMessage = $"Link to the last log: {reportJson.ConfigAwarePermalink}";
            if (_lastLogBossId != bossId)
            {
                _lastLogPullCounter = 0;
            }
            _lastLogBossId = bossId;
            _lastLogPullCounter = success ? 0 : _lastLogPullCounter + 1;

            AddToText($">: {reportJson.ConfigAwarePermalink}");

            // UI Log Item
            var uploadItem = new LogUploadItem
            {
                Timestamp = DateTime.Now,
                BossName = reportJson.ExtraJson?.FightName ?? reportJson.Encounter.Boss,
                BossId = bossId,
                Success = success,
                Duration = reportJson.ExtraJson?.Duration ?? $"{reportJson.Encounter.Duration}s",
                Permalink = reportJson.ConfigAwarePermalink,
                IsCm = reportJson.ChallengeMode,
                IsLegendaryCm = reportJson.LegendaryChallengeMode,
                LocalFilePath = file,
            };

            Dispatcher.UIThread.Post(() =>
            {
                RecentUploads.Insert(0, uploadItem);
                if (RecentUploads.Count > 100)
                {
                    RecentUploads.RemoveAt(RecentUploads.Count - 1);
                }
            });

            // Dispatch to Integrations
            var players = reportJson.GetLogPlayers();

            _ = SendLogToTwitchChatAsync(reportJson, bypassMessage);
            _ = DiscordWebhookService.ExecuteAllActiveDiscordWebhooksAsync(this, reportJson, players);
            _ = ExecuteAllPingsAsync(reportJson);
            _ = ExecuteAllAleevaAsync(reportJson, players);
            _ = PostLogToGw2BotAsync(reportJson, players);
            _ = CrossUploadToWingmanAsync(file, reportJson?.ExtraJson);

            AddToText($">: {fileName} successfully uploaded.");
            LogReuploader.RemoveLogAndSave(file);
        }
        catch (Exception ex)
        {
            AddToText($">: Error uploading {fileName}: {ex.Message}");
            await HandleQuickRetryAsync(file, postData, bypassMessage);
        }
    }

    private async Task HandleQuickRetryAsync(string file, Dictionary<string, string> postData, bool bypassMessage)
    {
        if (_uploadFailCounters.TryGetValue(file, out var count))
        {
            if (count > 4)
            {
                _uploadFailCounters.Remove(file);
                AddToText($">: Upload failed 4 times for {Path.GetFileName(file)}. Queued for later reupload.");
                LogReuploader.FailedLogs.Add(file);
                LogReuploader.SaveFailedLogs();
                _failedLogsTimer.Start();
            }
            else
            {
                _uploadFailCounters[file]++;
                var delay = _uploadFailCounters[file] switch
                {
                    4 => 180000,
                    3 => 90000,
                    2 => 30000,
                    _ => 3000,
                };
                AddToText($">: Retrying in {delay / 1000}s...");
                await Task.Delay(delay);
                await UploadLogFileAsync(file, postData, bypassMessage);
            }
        }
        else
        {
            _uploadFailCounters.Add(file, 1);
            AddToText(">: Retrying in 3s...");
            await Task.Delay(3000);
            await UploadLogFileAsync(file, postData, bypassMessage);
        }
    }

    private string CreateDpsReportLink()
    {
        var settings = ApplicationSettings.Current.Upload;
        var token = settings.DpsReportUserTokens.Find(x => x.Active)?.UserToken ?? "";
        var sb = new StringBuilder(settings.DpsReportServerLink);
        sb.Append("/uploadContent");

        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(token)) queryParams.Add($"userToken={token}");
        if (settings.Anonymous) queryParams.Add("anonymous=true");
        if (settings.DetailedWvW) queryParams.Add("detailedwvw=true");

        if (queryParams.Count > 0)
        {
            sb.Append('?');
            sb.Append(string.Join('&', queryParams));
        }

        return sb.ToString();
    }

    private void TimerFailedLogsElapsed(object sender, ElapsedEventArgs e)
    {
        if (LogReuploader.FailedLogs.Count == 0)
        {
            _failedLogsTimer.Stop();
            return;
        }

        LogReuploader.ProcessLogs(_semaphore, async (file, data, bypass) =>
        {
            await UploadLogFileAsync(file, data, bypass);
        });
    }

    #region Integrations Dispatch

    private async Task SendLogToTwitchChatAsync(DpsReportJson reportJson, bool bypassMessage)
    {
        if (!ApplicationSettings.Current.Twitch.ConnectToTwitch || _twitchClient is null)
        {
            return;
        }

        if (ApplicationSettings.Current.Upload.PostLogsToTwitchOnlyWithStreamingSoftware && !IsStreamingSoftwareRunning())
        {
            return;
        }

        var bossData = Bosses.GetBossDataFromId(reportJson.ExtraJson?.TriggerId ?? reportJson.Encounter.BossId);
        if (bossData is null)
        {
            LastLogMessage = $"Link to the last log: {reportJson.ConfigAwarePermalink}";
            await _twitchClient.SendChatMessageAsync(ApplicationSettings.Current.Twitch.ChannelName, LastLogMessage);
            return;
        }

        var format = bossData.TwitchMessageFormat(reportJson, _lastLogPullCounter);
        if (!string.IsNullOrWhiteSpace(format) && !bypassMessage)
        {
            LastLogMessage = format;
            await _twitchClient.SendChatMessageAsync(ApplicationSettings.Current.Twitch.ChannelName, LastLogMessage);
        }
    }

    private static bool IsStreamingSoftwareRunning()
    {
        var streamApps = new[] { "obs", "obs64", "streamlabs", "twitchstudio" };
        return Process.GetProcesses().Any(p =>
        {
            try
            {
                var name = p.ProcessName.ToLowerInvariant();
                return streamApps.Any(app => name.Contains(app));
            }
            catch
            {
                return false;
            }
        });
    }

    private async Task ExecuteAllPingsAsync(DpsReportJson reportJson)
    {
        foreach (var ping in PingConfigurations.All.Values.Where(p => p.Active))
        {
            await ping.PingServerAsync(this, reportJson);
        }
    }

    private async Task ExecuteAllAleevaAsync(DpsReportJson reportJson, List<LogPlayer> players)
    {
        if (!ApplicationSettings.Current.Aleeva.Authorised)
        {
            return;
        }

        foreach (var integration in AleevaIntegrations.All.Where(i => i.Active))
        {
            await integration.PostLogToAleeva(this, HttpClientController, reportJson, players);
        }
    }

    private async Task PostLogToGw2BotAsync(DpsReportJson reportJson, List<LogPlayer> players)
    {
        var settings = ApplicationSettings.Current.Gw2Bot;
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return;
        }

        if (settings.SendOnSuccessOnly && !(reportJson.Encounter.Success ?? false))
        {
            return;
        }

        if (settings.TeamId > 0 && Teams.Teams.All.TryGetValue(settings.TeamId, out var team) && !team.IsSatisfied(players))
        {
            return;
        }

        try
        {
            _gw2BotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            var logObject = new Gw2BotAddReport { LogLink = reportJson.Permalink };
            var jsonLog = JsonConvert.SerializeObject(logObject);
            using var content = new StringContent(jsonLog, Encoding.UTF8, "application/json");
            using var response = await _gw2BotClient.PostAsync($"{Gw2BotApiBaseUrl}/evtc/notification", content);
            if (!response.IsSuccessStatusCode)
            {
                AddToText($"??> GW2Bot notification returned status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            AddToText($"??> GW2Bot notification failed: {ex.Message}");
        }
    }

    private async Task CrossUploadToWingmanAsync(string file, DpsReportJsonExtraJson extraJson)
    {
        if (!ApplicationSettings.Current.Upload.UploadToWingman || extraJson is null)
        {
            return;
        }

        var result = await _wingmanUploader.Upload(file, extraJson);
        AddToText(result);
    }

    #endregion

    #region Twitch Bot Lifecycle

    public async Task ConnectTwitchBotAsync()
    {
        try
        {
            DisconnectTwitchBot();

            var twitchSettings = ApplicationSettings.Current.Twitch;
            if (twitchSettings.Custom.Enabled)
            {
                _twitchClient = new TwitchChatClient(twitchSettings.Custom.Name, twitchSettings.Custom.OauthPassword);
            }
            else
            {
                _twitchClient = new TwitchChatClient("gw2loguploader", "oauth:ycgqr3dyef7gp5r8uk7d5jz30nbrc6");
            }

            _twitchClient.ReceivedMessage += OnTwitchMessageReceived;
            _twitchClient.StateChanged += OnTwitchStateChanged;
            await _twitchClient.BeginConnectionAsync();

            twitchSettings.ConnectToTwitch = true;
            ApplicationSettings.Current.Save();
            TwitchStateChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            AddToText($"<-?-> FAILED TO CONNECT TO TWITCH: {ex.Message}");
            TwitchStateChanged?.Invoke(false);
        }
    }

    public void DisconnectTwitchBot()
    {
        if (_twitchClient is not null)
        {
            _twitchClient.ReceivedMessage -= OnTwitchMessageReceived;
            _twitchClient.StateChanged -= OnTwitchStateChanged;
            _twitchClient.Dispose();
            _twitchClient = null;
            AddToText("<-?-> CONNECTION TO TWITCH CLOSED");

            ApplicationSettings.Current.Twitch.ConnectToTwitch = false;
            ApplicationSettings.Current.Save();
            TwitchStateChanged?.Invoke(false);
        }
    }

    private async void OnTwitchStateChanged(object sender, IrcState newState, string channelName)
    {
        if (newState == IrcState.Disconnected)
        {
            AddToText("<-?-> DISCONNECTED FROM TWITCH");
            _twitchReconnectFailCounter++;
            if (_twitchReconnectFailCounter <= 4)
            {
                AddToText($"<-?-> RECONNECTING TO TWITCH IN {_twitchReconnectFailCounter * 15}s");
                await Task.Delay(_twitchReconnectFailCounter * 15000);
                await ConnectTwitchBotAsync();
            }
            else
            {
                AddToText("<-?-> FAILED TO RECONNECT AFTER 4 ATTEMPTS.");
                DisconnectTwitchBot();
            }
            return;
        }

        if (newState == IrcState.Connected)
        {
            AddToText("<-?-> TWITCH CONNECTION ESTABLISHED");
            _twitchReconnectFailCounter = 0;
            if (!string.IsNullOrWhiteSpace(ApplicationSettings.Current.Twitch.ChannelName))
            {
                await _twitchClient.JoinRoomAsync(ApplicationSettings.Current.Twitch.ChannelName);
            }
            return;
        }

        if (newState == IrcState.ChannelJoined)
        {
            AddToText($"<-?-> TWITCH CHANNEL JOINED: {channelName}");
            _twitchCommandReplacements["%channel%"] = ApplicationSettings.Current.Twitch.ChannelName;
            return;
        }
    }

    private async void OnTwitchMessageReceived(object sender, IrcMessage ircMessage)
    {
        if (ircMessage?.IsChannelMessage != true)
        {
            return;
        }

        var commands = TwitchCommands.FindResponsesForInput(ircMessage.ChannelMessage);
        if (commands.Count == 0)
        {
            return;
        }

        foreach (var cmd in commands)
        {
            var senderName = ircMessage.UserName.Split('!')[0];
            AddToText($"> Twitch command used: {cmd.Name}");
            var reply = cmd.FormattedResponse(senderName, _twitchCommandReplacements);
            if (reply is null) continue;

            if (reply.Contains("%sender%")) reply = reply.Replace("%sender%", senderName);
            if (reply.Contains("%spotifySong%")) reply = reply.Replace("%spotifySong%", GetSpotifySong());
            if (reply.Contains("%gw2Build%")) reply = reply.Replace("%gw2Build%", await GenerateBuildCodeAsync() ?? "Could not obtain build code.");
            if (reply.Contains("%gw2Ign%")) reply = reply.Replace("%gw2Ign%", await GenerateIgnResponseAsync() ?? "Could not obtain in-game name.");

            await _twitchClient.SendChatMessageAsync(ApplicationSettings.Current.Twitch.ChannelName, reply);
        }
    }

    private static string GetSpotifySong()
    {
        try
        {
            var spotify = Process.GetProcesses().FirstOrDefault(p =>
                p.ProcessName.Contains("spotify", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(p.MainWindowTitle));
            if (spotify is not null)
            {
                return spotify.MainWindowTitle.Contains("Spotify") ? "No song playing." : spotify.MainWindowTitle;
            }
        }
        catch { }
        return "Spotify not running.";
    }

    private async Task<string> GenerateIgnResponseAsync()
    {
        MumbleReader?.Update();
        var charName = MumbleReader?.Data.Identity?.Name;
        if (string.IsNullOrWhiteSpace(charName)) return null;

        var apiKey = ApplicationSettings.Current.Gw2Apis.Find(x => x.Valid && x.Characters.Contains(charName));
        if (apiKey is null) return null;

        using var helper = new Gw2ApiHelper(apiKey.ApiKey);
        var account = await helper.GetUserInfoAsync();
        return account?.Name;
    }

    private async Task<string> GenerateBuildCodeAsync()
    {
        MumbleReader?.Update();
        var charName = MumbleReader?.Data.Identity?.Name;
        if (string.IsNullOrWhiteSpace(charName)) return null;

        var apiKey = ApplicationSettings.Current.Gw2Apis.Find(x => x.Valid && x.Characters.Contains(charName));
        if (apiKey is null) return null;

        try
        {
            var code = await APILoader.LoadBuildCodeFromCurrentCharacter(apiKey.ApiKey);
            Static.Compress(code, ApplicationSettings.Current.BuildCodes.Compression);
            return $"https://hardstuck.gg/gw2/builds/?b={TextLoader.WriteBuildCode(code)}";
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Log Session Controls

    public void StartSession()
    {
        IsSessionRunning = true;
        IsSessionPaused = false;
        SessionLogs.Clear();
        AddToText(">>> Log session started.");
        SessionStateChanged?.Invoke(true, false);
    }

    public void PauseSession()
    {
        if (!IsSessionRunning) return;
        IsSessionPaused = !IsSessionPaused;
        AddToText(IsSessionPaused ? ">>> Log session paused." : ">>> Log session resumed.");
        SessionStateChanged?.Invoke(true, IsSessionPaused);
    }

    public async Task StopSessionAsync(LogSessionSettings settings = null)
    {
        if (!IsSessionRunning) return;

        IsSessionRunning = false;
        IsSessionPaused = false;
        AddToText(">>> Log session finished.");
        SessionStateChanged?.Invoke(false, false);

        if (SessionLogs.Count > 0 && settings is not null)
        {
            await DiscordWebhookService.ExecuteSessionWebhooksAsync(this, SessionLogs, settings);

            if (settings.SaveToFile)
            {
                var sessionCsv = Path.Combine(ApplicationSettings.LocalDir, $"{settings.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                var sb = new StringBuilder("Boss;BossId;Success;Duration;RecordedBy;EliteInsightsVersion;arcdpsVersion;Permalink;UserToken\n");
                foreach (var log in SessionLogs)
                {
                    sb.Append($"{log.ExtraJson?.FightName ?? log.Encounter.Boss};{log.Encounter.BossId};{(log.Encounter.Success ?? false ? "true" : "false")};{log.ExtraJson?.Duration ?? ""};{log.ExtraJson?.RecordedByAccountName ?? ""};{log.ExtraJson?.EliteInsightsVersion ?? ""};{log.Evtc.Type}{log.Evtc.Version};{log.ConfigAwarePermalink};{log.UserToken}\n");
                }
                File.WriteAllText(sessionCsv, sb.ToString());
                AddToText($">>> Saved session logs to: {sessionCsv}");
            }
        }

        SessionLogs.Clear();
    }

    #endregion

    #region ArcDPS Plugins

    public async Task<List<ArcDpsComponent>> CheckPluginUpdatesAsync()
    {
        var gw2Dir = ApplicationSettings.Current.Gw2Location;
        var components = ArcDpsComponent.DeserialiseAll(ApplicationSettings.LocalDir);

        foreach (var component in components)
        {
            try
            {
                if (component.Type == ArcDpsComponentType.ArcDps)
                {
                    // Check deltaconnected md5
                    var latestHash = await HttpClientController.DownloadFileToStringAsync(component.VersionLink);
                    // Compare local md5 if file exists
                }
                else if (!string.IsNullOrWhiteSpace(component.Repository))
                {
                    var release = await HttpClientController.GetGitHubLatestReleaseAsync(component.Repository);
                    // Update latest release info
                }
            }
            catch { }
        }

        return components;
    }

    public async Task<bool> InstallOrUpdatePluginAsync(ArcDpsComponent component)
    {
        return await component.DownloadComponent(HttpClientController);
    }

    #endregion

    #region Updates & Utilities

    public async Task CheckForUpdatesAsync(bool quiet = false)
    {
        try
        {
            var versionStr = await HttpClientController.DownloadFileToStringAsync(PlenBotVersionFileUrl);
            if (int.TryParse(versionStr?.Trim(), out var onlineVersion))
            {
                if (onlineVersion > ApplicationSettings.Version)
                {
                    AddToText($">>> An update is available! Version: {onlineVersion} (Current: {ApplicationSettings.Version})");
                    ShowBalloon("PlenBot Log Uploader Update", $"Version {onlineVersion} is available!", 7500);
                }
                else if (!quiet)
                {
                    AddToText(">>> PlenBot Log Uploader is up to date.");
                }
            }
        }
        catch (Exception ex)
        {
            if (!quiet) AddToText($">>> Could not check for updates: {ex.Message}");
        }
    }

    public void AddToText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var level = "Info";
        if (text.Contains("error", StringComparison.OrdinalIgnoreCase) || text.Contains("failed", StringComparison.OrdinalIgnoreCase))
            level = "Error";
        else if (text.Contains("warning", StringComparison.OrdinalIgnoreCase))
            level = "Warning";
        else if (text.Contains("successfully", StringComparison.OrdinalIgnoreCase) || text.Contains("up to date", StringComparison.OrdinalIgnoreCase))
            level = "Success";

        var item = new LogMessageItem
        {
            Timestamp = DateTime.Now,
            Message = text,
            Level = level,
        };

        Dispatcher.UIThread.Post(() =>
        {
            ActivityLogs.Add(item);
            if (ActivityLogs.Count > 500)
            {
                ActivityLogs.RemoveAt(0);
            }
        });

        Console.WriteLine($"[{item.TimestampString}] {text}");
    }

    public void ShowBalloon(string title, string description, int ms = 5000)
    {
        NotificationRequested?.Invoke(title, description, ms);
    }

    private void UpdateLogCount()
    {
        Dispatcher.UIThread.Post(() => LogsCountChanged?.Invoke(_logsCount));
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _failedLogsTimer.Dispose();
        _updateCheckTimer.Dispose();
        _semaphore.Dispose();
        _logPoster?.Dispose();
        _gw2BotClient?.Dispose();
        HttpClientController?.Dispose();
        _watcher?.Dispose();
        _twitchClient?.Dispose();
        MumbleReader?.Dispose();
    }

    #endregion
}
