using Newtonsoft.Json;
using PlenBotLogUploader.Tools;
using System;
using System.Collections.Generic;
using System.IO;

namespace PlenBotLogUploader.AppSettings;

public struct WindowSize
{
    public int Width { get; set; }
    public int Height { get; set; }

    public WindowSize(int width, int height)
    {
        Width = width;
        Height = height;
    }
}

public enum WindowStateSetting
{
    Normal = 0,
    Minimized = 1,
    Maximized = 2
}

[JsonObject(MemberSerialization.OptIn)]
internal sealed class ApplicationSettings
{
    private static readonly string _fileName = "app_settings.json";

    private static ApplicationSettings _current;

    internal ApplicationSettings()
    {
        _current = this;
    }

    internal static int Version => 106;

    internal static ApplicationSettings Current
    {
        get => _current ??= Load();
        private set => _current = value;
    }

    private static string _localDir;

    internal static string LocalDir
    {
        get
        {
            if (_localDir is not null)
            {
                return _localDir;
            }

            // On Linux or when running in an AppImage / flatpak, use user config dir ~/.config/PlenBotLogUploader
            if (!OperatingSystem.IsWindows() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPIMAGE")))
            {
                var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlenBotLogUploader");
                Directory.CreateDirectory(configDir);
                _localDir = configDir;
            }
            else
            {
                try
                {
                    // Check if application directory is writable (portable mode on Windows)
                    var testFile = Path.Combine(AppContext.BaseDirectory, ".write_test");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    _localDir = AppContext.BaseDirectory;
                }
                catch
                {
                    var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlenBotLogUploader");
                    Directory.CreateDirectory(configDir);
                    _localDir = configDir;
                }
            }

            return _localDir;
        }
        set => _localDir = value;
    }

    [JsonProperty("aleeva")]
    internal ApplicationSettingsAleeva Aleeva { get; set; } = new();

    [JsonProperty("arcUpdate")]
    internal ApplicationSettingsArcUpdate ArcUpdate { get; set; } = new();

    [JsonProperty("autoUpdate")]
    internal bool AutoUpdate { get; set; }

    [JsonProperty("bossTemplate")]
    internal ApplicationSettingsBossTemplate BossTemplate { get; set; } = new();

    [JsonProperty("buildCodes")]
    internal ApplicationSettingsBuildCodes BuildCodes { get; set; } = new();

    [JsonProperty("firstApplicationRun")]
    internal bool FirstApplicationRun { get; set; } = true;

    [JsonProperty("firstTimeMinimised")]
    internal bool FirstTimeMinimised { get; set; }

    [JsonProperty("gw2APIKeys")]
    internal List<ApplicationSettingsGw2Api> Gw2Apis { get; set; } = [];

    [JsonProperty("gw2Bot")]
    internal ApplicationSettingsGw2Bot Gw2Bot { get; set; } = new();

    [JsonProperty("gw2Location")]
    internal string Gw2Location { get; set; } = "";

    [JsonProperty("logsLocation")]
    internal string LogsLocation { get; set; } = "";

    [JsonProperty("usePollingForLogs")]
    internal bool UsePollingForLogs { get; set; }

    [JsonProperty("mainFormSize")]
    internal WindowSize MainFormSize { get; set; } = new(1000, 720);

    [JsonProperty("mainFormState")]
    internal WindowStateSetting MainFormState { get; set; } = WindowStateSetting.Normal;

    [JsonProperty("maxConcurrentUploads")]
    internal int MaxConcurrentUploads { get; set; } = 4;

    [JsonProperty("minimiseToTry")]
    internal bool MinimiseToTray { get; set; } = true;

    [JsonProperty("closeToTry")]
    internal bool CloseToTray { get; set; }

    [JsonProperty("session")]
    internal ApplicationSettingsSession Session { get; set; } = new();

    [JsonProperty("shortenThousands")]
    internal bool ShortenThousands { get; set; }

    [JsonProperty("twitch")]
    internal ApplicationSettingsTwitch Twitch { get; set; } = new();

    [JsonProperty("upload")]
    internal ApplicationSettingsUpload Upload { get; set; } = new();

    [JsonProperty("theme")]
    internal string Theme { get; set; } = "System";

    internal static bool IsRunningInWine { get; } = WineConnoisseur.IsRunningUnderWine();

    internal event EventHandler<EventArgs> SettingsSaved;

    private static void AppendSettingsCrashWithMessage(string message)
    {
        try
        {
            var crashLog = Path.Combine(LocalDir, "settings_crash.log");
            File.AppendAllText(crashLog, $"[{DateTime.Now}] {message}{Environment.NewLine}");
        }
        catch
        {
            Console.Error.WriteLine($"Settings crash: {message}");
        }
    }

    private void SerialiseToFile(string saveLocation)
    {
        SettingsSaved?.Invoke(this, EventArgs.Empty);
        try
        {
            File.WriteAllText(saveLocation, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        catch (Exception ex)
        {
            AppendSettingsCrashWithMessage(ex.Message);
        }
    }

    private static ApplicationSettings DeserialiseFromFile(string loadLocation)
    {
        if (File.Exists(loadLocation))
        {
            try
            {
                var json = File.ReadAllText(loadLocation);
                var settings = JsonConvert.DeserializeObject<ApplicationSettings>(json);
                if (settings is not null)
                {
                    _current = settings;
                    return settings;
                }
            }
            catch (Exception ex)
            {
                AppendSettingsCrashWithMessage(ex.Message);
            }
            try
            {
                File.Copy(loadLocation, Path.Combine(LocalDir, $"app_settings {DateTime.Now:yyyy-MM-dd HHmmss}.json"));
            }
            catch (Exception ex)
            {
                AppendSettingsCrashWithMessage(ex.Message);
            }
        }
        var sett = new ApplicationSettings();
        sett.Save();
        _current = sett;
        return sett;
    }

    internal static bool Exists() => File.Exists(Path.Combine(LocalDir, _fileName));
    internal static ApplicationSettings Load() => DeserialiseFromFile(Path.Combine(LocalDir, _fileName));
    internal void Save() => SerialiseToFile(Path.Combine(LocalDir, _fileName));
}
