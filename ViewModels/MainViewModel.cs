using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlenBotLogUploader.Aleeva;
using PlenBotLogUploader.AppSettings;
using PlenBotLogUploader.ArcDps;
using PlenBotLogUploader.DiscordApi;
using PlenBotLogUploader.DpsReport;
using PlenBotLogUploader.RemotePing;
using PlenBotLogUploader.Services;
using PlenBotLogUploader.Teams;
using PlenBotLogUploader.Tools;
using PlenBotLogUploader.Twitch;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PlenBotLogUploader.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly LogUploaderService _service;

    [ObservableProperty]
    private int _selectedNavigationIndex = 0;

    [ObservableProperty]
    private string _logsLocation;

    [ObservableProperty]
    private string _gw2Location;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private bool _isSessionRunning;

    [ObservableProperty]
    private bool _isSessionPaused;

    [ObservableProperty]
    private bool _isTwitchConnected;

    public string MonitoringButtonText => IsMonitoring ? "Stop Monitoring" : "Start Monitoring";
    public string SessionButtonText => IsSessionRunning ? "End Session" : "Start Session";
    public string SessionPauseButtonText => IsSessionPaused ? "Resume Session" : "Pause Session";
    public string TwitchConnectButtonText => IsTwitchConnected ? "Disconnect" : "Connect";
    public string MonitoringStatusText => IsMonitoring ? "Monitoring: Active" : "Monitoring: Stopped";
    public string MonitoringBadgeBackground => IsMonitoring ? "#2E7D32" : "#757575";
    public bool HasNoLogsLocation => string.IsNullOrWhiteSpace(LogsLocation) || !Directory.Exists(LogsLocation);
    public string SessionStatusText => IsSessionRunning 
        ? (IsSessionPaused ? "Session: Paused" : "Session: Active") 
        : "Session: Inactive";

    public bool HasRecentUploads => RecentUploads.Count > 0;
    public bool HasNoRecentUploads => RecentUploads.Count == 0;
    public bool HasDiscordWebhooks => DiscordWebhooksList.Count > 0;
    public bool HasNoDiscordWebhooks => DiscordWebhooksList.Count == 0;
    public bool HasTeams => TeamsList.Count > 0;
    public bool HasNoTeams => TeamsList.Count == 0;
    public bool HasGw2ApiKeys => Gw2ApiKeysList.Count > 0;
    public bool HasNoGw2ApiKeys => Gw2ApiKeysList.Count == 0;
    public bool HasRemotePings => RemotePingsList.Count > 0;
    public bool HasNoRemotePings => RemotePingsList.Count == 0;
    public bool HasAleevaIntegrations => AleevaIntegrationsList.Count > 0;
    public bool HasNoAleevaIntegrations => AleevaIntegrationsList.Count == 0;
    public bool HasUserTokens => UserTokensList.Count > 0;
    public bool HasNoUserTokens => UserTokensList.Count == 0;

    partial void OnIsMonitoringChanged(bool value)
    {
        OnPropertyChanged(nameof(MonitoringButtonText));
        OnPropertyChanged(nameof(MonitoringStatusText));
        OnPropertyChanged(nameof(MonitoringBadgeBackground));
    }

    partial void OnLogsLocationChanged(string value)
    {
        OnPropertyChanged(nameof(HasNoLogsLocation));
    }

    partial void OnIsSessionRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(SessionButtonText));
        OnPropertyChanged(nameof(SessionStatusText));
    }

    partial void OnIsSessionPausedChanged(bool value)
    {
        OnPropertyChanged(nameof(SessionPauseButtonText));
        OnPropertyChanged(nameof(SessionStatusText));
    }

    partial void OnIsTwitchConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(TwitchConnectButtonText));
    }

    [ObservableProperty]
    private int _logsCount;

    [ObservableProperty]
    private string _sessionName = "Raid Session";

    [ObservableProperty]
    private string _sessionMessage = "";

    // Settings
    [ObservableProperty]
    private bool _startupWithOs;

    [ObservableProperty]
    private bool _minimiseToTray;

    [ObservableProperty]
    private bool _closeToTray;

    [ObservableProperty]
    private bool _usePolling;

    [ObservableProperty]
    private bool _saveToCsv;

    [ObservableProperty]
    private bool _anonymousUpload;

    [ObservableProperty]
    private bool _detailedWvW;

    [ObservableProperty]
    private bool _uploadToWingman;

    [ObservableProperty]
    private int _maxConcurrentUploads = 4;

    [ObservableProperty]
    private string _selectedTheme = "System";

    // Twitch Settings
    [ObservableProperty]
    private string _twitchChannelName = "";

    [ObservableProperty]
    private bool _twitchPostLogs;

    [ObservableProperty]
    private bool _twitchOnlySuccess;

    [ObservableProperty]
    private bool _twitchOnlyStreamRunning;

    [ObservableProperty]
    private bool _twitchCustomBot;

    [ObservableProperty]
    private string _twitchCustomBotName = "";

    [ObservableProperty]
    private string _twitchCustomBotToken = "";

    // Aleeva Settings
    [ObservableProperty]
    private string _aleevaApiKey = "";

    [ObservableProperty]
    private bool _aleevaAuthorized;

    // GW2Bot Settings
    [ObservableProperty]
    private bool _gw2BotEnabled;

    [ObservableProperty]
    private string _gw2BotApiKey = "";

    [ObservableProperty]
    private bool _gw2BotOnlySuccess;

    // Observable Collections
    public ObservableCollection<LogUploadItem> RecentUploads => _service.RecentUploads;
    public ObservableCollection<LogMessageItem> ActivityLogs => _service.ActivityLogs;
    public ObservableCollection<DiscordWebhookData> DiscordWebhooksList { get; } = [];
    public ObservableCollection<Team> TeamsList { get; } = [];
    public ObservableCollection<PingConfiguration> RemotePingsList { get; } = [];
    public ObservableCollection<AleevaIntegration> AleevaIntegrationsList { get; } = [];
    public ObservableCollection<ApplicationSettingsGw2Api> Gw2ApiKeysList { get; } = [];
    public ObservableCollection<ArcDpsComponent> ArcDpsComponentsList { get; } = [];
    public ObservableCollection<ApplicationSettingsUploadUserToken> UserTokensList { get; } = [];
    public ObservableCollection<TwitchCommand> TwitchCommandsList { get; } = [];

    // Navigation events for dialogs
    public Func<DiscordWebhookData, Task<DiscordWebhookData>> RequestEditWebhookDialog { get; set; }
    public Func<Team, Task<Team>> RequestEditTeamDialog { get; set; }
    public Func<PingConfiguration, Task<PingConfiguration>> RequestEditPingDialog { get; set; }
    public Func<AleevaIntegration, Task<AleevaIntegration>> RequestEditAleevaDialog { get; set; }
    public Func<ApplicationSettingsGw2Api, Task<ApplicationSettingsGw2Api>> RequestEditGw2ApiDialog { get; set; }
    public Func<ApplicationSettingsUploadUserToken, Task<ApplicationSettingsUploadUserToken>> RequestEditUserTokenDialog { get; set; }
    public Func<Task<string>> RequestBrowseFolderDialog { get; set; }
    public Func<Task<string>> RequestBrowseGw2FolderDialog { get; set; }
    public Action<string> RequestCopyToClipboard { get; set; }

    internal LogUploaderService Service => _service;

    public MainViewModel()
    {
        _service = LogUploaderService.Instance ?? new LogUploaderService();

        // Wire service events
        _service.MonitoringStateChanged += val => IsMonitoring = val;
        _service.SessionStateChanged += (running, paused) =>
        {
            IsSessionRunning = running;
            IsSessionPaused = paused;
        };
        _service.TwitchStateChanged += val => IsTwitchConnected = val;
        _service.LogsCountChanged += val => LogsCount = val;

        RecentUploads.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasRecentUploads));
            OnPropertyChanged(nameof(HasNoRecentUploads));
        };
        DiscordWebhooksList.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasDiscordWebhooks));
            OnPropertyChanged(nameof(HasNoDiscordWebhooks));
        };
        TeamsList.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasTeams));
            OnPropertyChanged(nameof(HasNoTeams));
        };
        Gw2ApiKeysList.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasGw2ApiKeys));
            OnPropertyChanged(nameof(HasNoGw2ApiKeys));
        };
        RemotePingsList.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasRemotePings));
            OnPropertyChanged(nameof(HasNoRemotePings));
        };
        AleevaIntegrationsList.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasAleevaIntegrations));
            OnPropertyChanged(nameof(HasNoAleevaIntegrations));
        };
        UserTokensList.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasUserTokens));
            OnPropertyChanged(nameof(HasNoUserTokens));
        };

        LoadSettingsAndData();
    }

    private void LoadSettingsAndData()
    {
        var settings = ApplicationSettings.Current;
        LogsLocation = settings.LogsLocation;
        Gw2Location = settings.Gw2Location;
        IsMonitoring = _service.IsMonitoring;
        LogsCount = _service.LogsCount;

        MinimiseToTray = settings.MinimiseToTray;
        CloseToTray = settings.CloseToTray;
        UsePolling = settings.UsePollingForLogs;
        SaveToCsv = settings.Upload.SaveToCsvEnabled;
        AnonymousUpload = settings.Upload.Anonymous;
        DetailedWvW = settings.Upload.DetailedWvW;
        UploadToWingman = settings.Upload.UploadToWingman;
        MaxConcurrentUploads = settings.MaxConcurrentUploads;
        SelectedTheme = settings.Theme ?? "System";
        StartupWithOs = StartupManager.IsStartupEnabled();

        // Twitch
        TwitchChannelName = settings.Twitch.ChannelName;
        TwitchPostLogs = settings.Upload.PostLogsToTwitch;
        TwitchOnlySuccess = settings.Twitch.ConnectToTwitch;
        TwitchOnlyStreamRunning = settings.Upload.PostLogsToTwitchOnlyWithStreamingSoftware;
        TwitchCustomBot = settings.Twitch.Custom.Enabled;
        TwitchCustomBotName = settings.Twitch.Custom.Name;
        TwitchCustomBotToken = settings.Twitch.Custom.OauthPassword;

        // Aleeva
        AleevaApiKey = settings.Aleeva.ApiKey;
        AleevaAuthorized = settings.Aleeva.Authorised;

        // GW2Bot
        Gw2BotEnabled = settings.Gw2Bot.Enabled;
        Gw2BotApiKey = settings.Gw2Bot.ApiKey;
        Gw2BotOnlySuccess = settings.Gw2Bot.SendOnSuccessOnly;

        RefreshCollections();
    }

    public void RefreshCollections()
    {
        DiscordWebhooksList.Clear();
        foreach (var webhook in DiscordWebhooks.All.Values)
        {
            DiscordWebhooksList.Add(webhook);
        }

        TeamsList.Clear();
        foreach (var team in Teams.Teams.All.Values)
        {
            TeamsList.Add(team);
        }

        RemotePingsList.Clear();
        foreach (var ping in PingConfigurations.All.Values)
        {
            RemotePingsList.Add(ping);
        }

        AleevaIntegrationsList.Clear();
        foreach (var integration in AleevaIntegrations.All)
        {
            AleevaIntegrationsList.Add(integration);
        }

        Gw2ApiKeysList.Clear();
        foreach (var key in ApplicationSettings.Current.Gw2Apis)
        {
            Gw2ApiKeysList.Add(key);
        }

        UserTokensList.Clear();
        foreach (var token in ApplicationSettings.Current.Upload.DpsReportUserTokens)
        {
            UserTokensList.Add(token);
        }

        ArcDpsComponentsList.Clear();
        foreach (var component in ArcDpsComponent.All)
        {
            ArcDpsComponentsList.Add(component);
        }

        TwitchCommandsList.Clear();
        foreach (var cmd in TwitchCommands.All)
        {
            TwitchCommandsList.Add(cmd);
        }
    }

    [RelayCommand]
    private async Task ToggleMonitoring()
    {
        if (IsMonitoring)
        {
            _service.StopMonitoring();
            return;
        }

        if (string.IsNullOrWhiteSpace(LogsLocation) || !Directory.Exists(LogsLocation))
        {
            _service.AddToText(">>> No arcDPS logs folder set. Please select the folder containing your .evtc/.zevtc logs.");
            await BrowseLogsFolderAsync();
            if (string.IsNullOrWhiteSpace(LogsLocation) || !Directory.Exists(LogsLocation))
            {
                _service.AddToText(">>> Monitoring not started: No folder selected.");
                return;
            }
        }
        else
        {
            _service.StartMonitoring(LogsLocation);
        }
    }

    [RelayCommand]
    private async Task BrowseLogsFolderAsync()
    {
        if (RequestBrowseFolderDialog is not null)
        {
            var folder = await RequestBrowseFolderDialog.Invoke();
            if (!string.IsNullOrWhiteSpace(folder))
            {
                if (File.Exists(folder))
                {
                    folder = Path.GetDirectoryName(folder) ?? folder;
                }

                if (Directory.Exists(folder))
                {
                    LogsLocation = folder;
                    ApplicationSettings.Current.LogsLocation = folder;
                    ApplicationSettings.Current.Save();

                    _service.StartMonitoring(folder);
                }
            }
        }
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        if (!string.IsNullOrWhiteSpace(LogsLocation) && Directory.Exists(LogsLocation))
        {
            ProcessHelper.OpenDirectory(LogsLocation);
        }
        else
        {
            _service.AddToText(">>> Cannot open folder: No valid arcDPS combat logs folder set.");
        }
    }

    [RelayCommand]
    private async Task BrowseGw2FolderAsync()
    {
        if (RequestBrowseGw2FolderDialog is not null)
        {
            var folder = await RequestBrowseGw2FolderDialog.Invoke();
            if (!string.IsNullOrWhiteSpace(folder))
            {
                Gw2Location = folder;
                ApplicationSettings.Current.Gw2Location = folder;
                ApplicationSettings.Current.Save();
                _service.AddToText($">>> Guild Wars 2 directory set to: {folder}");
            }
        }
    }

    [RelayCommand]
    private async Task CheckArcUpdatesAsync()
    {
        _service.AddToText(">>> Checking for arcDPS and plugin updates...");
        foreach (var component in ArcDpsComponent.All)
        {
            try
            {
                var version = await component.GetVersionStringAsync(_service.HttpClientController);
                if (!component.IsCurrentVersion(version))
                {
                    _service.AddToText($">>> Updating {component.Type}...");
                    var downloaded = await component.DownloadComponent(_service.HttpClientController);
                    _service.AddToText(downloaded ? $">>> {component.Type} updated successfully!" : $">>> Failed to download update for {component.Type}.");
                }
            }
            catch (Exception ex)
            {
                _service.AddToText($">>> Error checking {component.Type}: {ex.Message}");
            }
        }
        _service.AddToText(">>> arcDPS plugin update check complete.");
        RefreshCollections();
    }

    [RelayCommand]
    private void ToggleSession()
    {
        if (!IsSessionRunning)
        {
            _service.StartSession();
        }
        else
        {
            _ = StopSessionAsync();
        }
    }

    [RelayCommand]
    private void TogglePauseSession()
    {
        _service.PauseSession();
    }

    [RelayCommand]
    private async Task StopSessionAsync()
    {
        var settings = new LogSessionSettings
        {
            Name = SessionName,
            ContentText = SessionMessage,
            SaveToFile = ApplicationSettings.Current.Session.SaveToFile,
        };
        await _service.StopSessionAsync(settings);
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        ProcessHelper.OpenUrl("https://github.com/Plenyx/PlenBotLogUploader");
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _service.ActivityLogs.Clear();
    }

    [RelayCommand]
    private void CopyLogs()
    {
        var text = string.Join(Environment.NewLine, _service.ActivityLogs.Select(l => $"[{l.TimestampString}] {l.Message}"));
        RequestCopyToClipboard?.Invoke(text);
        _service.AddToText(">>> Logs copied to clipboard.");
    }

    [RelayCommand]
    private async Task ToggleTwitchAsync()
    {
        if (IsTwitchConnected)
        {
            _service.DisconnectTwitchBot();
        }
        else
        {
            SaveTwitchSettings();
            await _service.ConnectTwitchBotAsync();
        }
    }

    [RelayCommand]
    private void SaveTwitchSettings()
    {
        var twitch = ApplicationSettings.Current.Twitch;
        twitch.ChannelName = TwitchChannelName?.Trim() ?? "";
        ApplicationSettings.Current.Upload.PostLogsToTwitch = TwitchPostLogs;
        ApplicationSettings.Current.Upload.PostLogsToTwitchOnlyWithStreamingSoftware = TwitchOnlyStreamRunning;

        twitch.Custom.Enabled = TwitchCustomBot;
        twitch.Custom.Name = TwitchCustomBotName?.Trim() ?? "";
        twitch.Custom.OauthPassword = TwitchCustomBotToken?.Trim() ?? "";

        ApplicationSettings.Current.Save();
        _service.AddToText(">>> Twitch settings saved.");
    }

    [RelayCommand]
    private void SaveTwitchCommands()
    {
        TwitchCommands.Save();
        _service.AddToText(">>> Twitch chatbot commands saved.");
    }

    [RelayCommand]
    private async Task VerifyAleevaApiKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(AleevaApiKey))
        {
            _service.AddToText(">>> Aleeva API key is empty.");
            return;
        }

        _service.AddToText(">>> Verifying Aleeva API key...");
        await AleevaStatics.VerifyAleevaApiKey(_service, _service.HttpClientController, AleevaApiKey.Trim());
        AleevaAuthorized = ApplicationSettings.Current.Aleeva.Authorised;
        if (AleevaAuthorized)
        {
            _service.AddToText(">>> Aleeva API key verified successfully!");
        }
    }

    [RelayCommand]
    private void SaveGw2BotSettings()
    {
        var gw2bot = ApplicationSettings.Current.Gw2Bot;
        gw2bot.Enabled = Gw2BotEnabled;
        gw2bot.ApiKey = Gw2BotApiKey?.Trim() ?? "";
        gw2bot.SendOnSuccessOnly = Gw2BotOnlySuccess;
        ApplicationSettings.Current.Save();
        _service.AddToText(">>> GW2Bot settings saved.");
    }

    #region Webhook Management

    [RelayCommand]
    private async Task AddWebhookAsync()
    {
        if (RequestEditWebhookDialog is not null)
        {
            var newWebhook = new DiscordWebhookData
            {
                Active = true,
                Name = "New Webhook",
                Url = "",
            };
            var result = await RequestEditWebhookDialog.Invoke(newWebhook);
            if (result is not null && !string.IsNullOrWhiteSpace(result.Url))
            {
                var nextKey = DiscordWebhooks.All.Count > 0 ? DiscordWebhooks.All.Keys.Max() + 1 : 1;
                DiscordWebhooks.All[nextKey] = result;
                DiscordWebhooks.SaveToJson(DiscordWebhooks.All, DiscordWebhooks.JsonFileLocation);
                RefreshCollections();
                _service.AddToText($">>> Added Discord webhook: {result.Name}");
            }
        }
    }

    [RelayCommand]
    private async Task EditWebhookAsync(DiscordWebhookData webhook)
    {
        if (webhook is null || RequestEditWebhookDialog is null) return;
        var result = await RequestEditWebhookDialog.Invoke(webhook);
        if (result is not null)
        {
            DiscordWebhooks.SaveToJson(DiscordWebhooks.All, DiscordWebhooks.JsonFileLocation);
            RefreshCollections();
            _service.AddToText($">>> Updated Discord webhook: {result.Name}");
        }
    }

    [RelayCommand]
    private void DeleteWebhook(DiscordWebhookData webhook)
    {
        if (webhook is null) return;
        var pair = DiscordWebhooks.All.FirstOrDefault(x => x.Value == webhook);
        if (pair.Value is not null)
        {
            DiscordWebhooks.All.Remove(pair.Key);
            DiscordWebhooks.SaveToJson(DiscordWebhooks.All, DiscordWebhooks.JsonFileLocation);
            RefreshCollections();
            _service.AddToText($">>> Removed Discord webhook: {webhook.Name}");
        }
    }

    [RelayCommand]
    private async Task TestWebhookAsync(DiscordWebhookData webhook)
    {
        if (webhook is null || string.IsNullOrWhiteSpace(webhook.Url)) return;
        _service.AddToText($">>> Testing Discord webhook '{webhook.Name}'...");
        var success = await webhook.TestWebhookAsync(_service.HttpClientController);
        _service.AddToText(success ? $">>> Webhook '{webhook.Name}' is working properly!" : $">>> Webhook '{webhook.Name}' failed test.");
    }

    #endregion

    #region Teams Management

    [RelayCommand]
    private async Task AddTeamAsync()
    {
        if (RequestEditTeamDialog is not null)
        {
            var newTeam = new Team
            {
                Name = "New Team",
                Limiter = TeamLimiter.OR,
            };
            var result = await RequestEditTeamDialog.Invoke(newTeam);
            if (result is not null && !string.IsNullOrWhiteSpace(result.Name))
            {
                var nextKey = Teams.Teams.All.Count > 0 ? Teams.Teams.All.Keys.Max() + 1 : 1;
                result.Id = nextKey;
                Teams.Teams.All[nextKey] = result;
                Teams.Teams.SaveToJson(Teams.Teams.All);
                RefreshCollections();
                _service.AddToText($">>> Added Team: {result.Name}");
            }
        }
    }

    [RelayCommand]
    private async Task EditTeamAsync(Team team)
    {
        if (team is null || RequestEditTeamDialog is null) return;
        var result = await RequestEditTeamDialog.Invoke(team);
        if (result is not null)
        {
            Teams.Teams.SaveToJson(Teams.Teams.All);
            RefreshCollections();
            _service.AddToText($">>> Updated Team: {result.Name}");
        }
    }

    [RelayCommand]
    private void DeleteTeam(Team team)
    {
        if (team is null) return;
        Teams.Teams.All.Remove(team.Id);
        Teams.Teams.SaveToJson(Teams.Teams.All);
        RefreshCollections();
        _service.AddToText($">>> Removed Team: {team.Name}");
    }

    #endregion

    #region Remote Pings Management

    [RelayCommand]
    private async Task AddPingAsync()
    {
        if (RequestEditPingDialog is not null)
        {
            var newPing = new PingConfiguration
            {
                Active = true,
                Name = "New Remote Ping",
                Url = "https://example.com/api/ping",
                Method = PingMethod.Post,
            };
            var result = await RequestEditPingDialog.Invoke(newPing);
            if (result is not null && !string.IsNullOrWhiteSpace(result.Url))
            {
                var nextKey = PingConfigurations.All.Count > 0 ? PingConfigurations.All.Keys.Max() + 1 : 1;
                PingConfigurations.All[nextKey] = result;
                PingConfigurations.SaveToJson(PingConfigurations.All.Values);
                RefreshCollections();
                _service.AddToText($">>> Added Remote Ping: {result.Name}");
            }
        }
    }

    [RelayCommand]
    private async Task EditPingAsync(PingConfiguration ping)
    {
        if (ping is null || RequestEditPingDialog is null) return;
        var result = await RequestEditPingDialog.Invoke(ping);
        if (result is not null)
        {
            PingConfigurations.SaveToJson(PingConfigurations.All.Values);
            RefreshCollections();
            _service.AddToText($">>> Updated Remote Ping: {result.Name}");
        }
    }

    [RelayCommand]
    private void DeletePing(PingConfiguration ping)
    {
        if (ping is null) return;
        var pair = PingConfigurations.All.FirstOrDefault(x => x.Value == ping);
        if (pair.Value is not null)
        {
            PingConfigurations.All.Remove(pair.Key);
            PingConfigurations.SaveToJson(PingConfigurations.All.Values);
            RefreshCollections();
            _service.AddToText($">>> Removed Remote Ping: {ping.Name}");
        }
    }

    [RelayCommand]
    private async Task TestPingAsync(PingConfiguration ping)
    {
        if (ping is null || string.IsNullOrWhiteSpace(ping.Url)) return;
        _service.AddToText($">>> Testing remote ping '{ping.Name}'...");
        try
        {
            var success = await ping.PingServerAsync(_service, null);
            _service.AddToText(success ? $">>> Remote ping '{ping.Name}' responded successfully!" : $">>> Remote ping '{ping.Name}' failed.");
        }
        catch (Exception ex)
        {
            _service.AddToText($">>> Remote ping error: {ex.Message}");
        }
    }

    #endregion

    #region Aleeva Integrations Management

    [RelayCommand]
    private async Task AddAleevaAsync()
    {
        if (RequestEditAleevaDialog is not null)
        {
            var newAleeva = new AleevaIntegration
            {
                Active = true,
                Name = "New Aleeva Integration",
                Server = "",
                Channel = "",
            };
            var result = await RequestEditAleevaDialog.Invoke(newAleeva);
            if (result is not null && !string.IsNullOrWhiteSpace(result.Server))
            {
                AleevaIntegrations.All.Add(result);
                AleevaIntegrations.SaveToJson(AleevaIntegrations.All);
                RefreshCollections();
                _service.AddToText($">>> Added Aleeva integration: {result.Name}");
            }
        }
    }

    [RelayCommand]
    private async Task EditAleevaAsync(AleevaIntegration aleeva)
    {
        if (aleeva is null || RequestEditAleevaDialog is null) return;
        var result = await RequestEditAleevaDialog.Invoke(aleeva);
        if (result is not null)
        {
            AleevaIntegrations.SaveToJson(AleevaIntegrations.All);
            RefreshCollections();
            _service.AddToText($">>> Updated Aleeva integration: {result.Name}");
        }
    }

    [RelayCommand]
    private void DeleteAleeva(AleevaIntegration aleeva)
    {
        if (aleeva is null) return;
        AleevaIntegrations.All.Remove(aleeva);
        AleevaIntegrations.SaveToJson(AleevaIntegrations.All);
        RefreshCollections();
        _service.AddToText($">>> Removed Aleeva integration: {aleeva.Name}");
    }

    #endregion

    #region GW2 API Management

    [RelayCommand]
    private async Task AddGw2ApiKeyAsync()
    {
        if (RequestEditGw2ApiDialog is not null)
        {
            var newKey = new ApplicationSettingsGw2Api
            {
                Name = "Account",
                ApiKey = "",
            };
            var result = await RequestEditGw2ApiDialog.Invoke(newKey);
            if (result is not null && !string.IsNullOrWhiteSpace(result.ApiKey))
            {
                ApplicationSettings.Current.Gw2Apis.Add(result);
                ApplicationSettings.Current.Save();
                RefreshCollections();
                _ = Task.Run(async () =>
                {
                    await result.ValidateToken(_service.HttpClientController);
                    await result.GetCharacters(_service.HttpClientController);
                    RefreshCollections();
                });
                _service.AddToText($">>> Added GW2 API key: {result.Name}");
            }
        }
    }

    [RelayCommand]
    private async Task EditGw2ApiKeyAsync(ApplicationSettingsGw2Api key)
    {
        if (key is null || RequestEditGw2ApiDialog is null) return;
        var result = await RequestEditGw2ApiDialog.Invoke(key);
        if (result is not null)
        {
            ApplicationSettings.Current.Save();
            RefreshCollections();
            _ = Task.Run(async () =>
            {
                await result.ValidateToken(_service.HttpClientController);
                await result.GetCharacters(_service.HttpClientController);
                RefreshCollections();
            });
            _service.AddToText($">>> Updated GW2 API key: {result.Name}");
        }
    }

    [RelayCommand]
    private void DeleteGw2ApiKey(ApplicationSettingsGw2Api key)
    {
        if (key is null) return;
        ApplicationSettings.Current.Gw2Apis.Remove(key);
        ApplicationSettings.Current.Save();
        RefreshCollections();
        _service.AddToText($">>> Removed GW2 API key: {key.Name}");
    }

    #endregion

    #region User Tokens Management

    [RelayCommand]
    private async Task AddUserTokenAsync()
    {
        if (RequestEditUserTokenDialog is not null)
        {
            var newToken = new ApplicationSettingsUploadUserToken
            {
                Name = "User Token",
                Active = true,
                UserToken = "",
            };
            var result = await RequestEditUserTokenDialog.Invoke(newToken);
            if (result is not null && !string.IsNullOrWhiteSpace(result.UserToken))
            {
                ApplicationSettings.Current.Upload.DpsReportUserTokens.Add(result);
                ApplicationSettings.Current.Save();
                RefreshCollections();
                _service.AddToText($">>> Added dps.report user token: {result.Name}");
            }
        }
    }

    [RelayCommand]
    private async Task EditUserTokenAsync(ApplicationSettingsUploadUserToken token)
    {
        if (token is null || RequestEditUserTokenDialog is null) return;
        var result = await RequestEditUserTokenDialog.Invoke(token);
        if (result is not null)
        {
            ApplicationSettings.Current.Save();
            RefreshCollections();
            _service.AddToText($">>> Updated dps.report user token: {result.Name}");
        }
    }

    [RelayCommand]
    private void DeleteUserToken(ApplicationSettingsUploadUserToken token)
    {
        if (token is null) return;
        ApplicationSettings.Current.Upload.DpsReportUserTokens.Remove(token);
        ApplicationSettings.Current.Save();
        RefreshCollections();
        _service.AddToText($">>> Removed dps.report user token: {token.Name}");
    }

    #endregion

    #region Settings Actions

    partial void OnStartupWithOsChanged(bool value)
    {
        StartupManager.SetStartup(value);
        _service.AddToText(value ? ">>> Launch at system startup enabled." : ">>> Launch at system startup disabled.");
    }

    partial void OnMinimiseToTrayChanged(bool value)
    {
        ApplicationSettings.Current.MinimiseToTray = value;
        ApplicationSettings.Current.Save();
    }

    partial void OnCloseToTrayChanged(bool value)
    {
        ApplicationSettings.Current.CloseToTray = value;
        ApplicationSettings.Current.Save();
    }

    partial void OnUsePollingChanged(bool value)
    {
        _service.ChangeMonitoringMode(value);
    }

    partial void OnSaveToCsvChanged(bool value)
    {
        ApplicationSettings.Current.Upload.SaveToCsvEnabled = value;
        ApplicationSettings.Current.Save();
    }

    partial void OnAnonymousUploadChanged(bool value)
    {
        ApplicationSettings.Current.Upload.Anonymous = value;
        ApplicationSettings.Current.Save();
    }

    partial void OnDetailedWvWChanged(bool value)
    {
        ApplicationSettings.Current.Upload.DetailedWvW = value;
        ApplicationSettings.Current.Save();
    }

    partial void OnUploadToWingmanChanged(bool value)
    {
        ApplicationSettings.Current.Upload.UploadToWingman = value;
        ApplicationSettings.Current.Save();
    }

    partial void OnMaxConcurrentUploadsChanged(int value)
    {
        ApplicationSettings.Current.MaxConcurrentUploads = Math.Clamp(value, 1, 10);
        ApplicationSettings.Current.Save();
    }

    partial void OnSelectedThemeChanged(string value)
    {
        ApplicationSettings.Current.Theme = value;
        ApplicationSettings.Current.Save();

        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = value switch
            {
                "Dark" => ThemeVariant.Dark,
                "Light" => ThemeVariant.Light,
                _ => ThemeVariant.Default,
            };
        }
    }

    [RelayCommand]
    private void OpenConfigFolder()
    {
        ProcessHelper.OpenDirectory(ApplicationSettings.LocalDir);
    }

    #endregion
}
