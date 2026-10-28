using Microsoft.Win32;
using System;
using System.IO;

namespace PlenBotLogUploader.Services;

internal static class StartupManager
{
    private const string AppName = "PlenBot Log Uploader";
    private const string LinuxDesktopFileName = "plenbotloguploader.desktop";

    public static bool IsStartupEnabled()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var registryRun = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                var value = registryRun?.GetValue(AppName);
                return value != null;
            }

            if (OperatingSystem.IsLinux())
            {
                var autostartDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart");
                var desktopFile = Path.Combine(autostartDir, LinuxDesktopFileName);
                return File.Exists(desktopFile);
            }
        }
        catch
        {
            // Ignore OS security or permission exceptions
        }

        return false;
    }

    public static void SetStartup(bool enable)
    {
        try
        {
            var exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "PlenBotLogUploader");

            if (OperatingSystem.IsWindows())
            {
                using var registryRun = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (registryRun != null)
                {
                    if (enable)
                    {
                        registryRun.SetValue(AppName, $"\"{exePath}\" -m");
                    }
                    else
                    {
                        registryRun.DeleteValue(AppName, false);
                    }
                }
                return;
            }

            if (OperatingSystem.IsLinux())
            {
                var autostartDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart");
                var desktopFile = Path.Combine(autostartDir, LinuxDesktopFileName);

                if (enable)
                {
                    Directory.CreateDirectory(autostartDir);
                    var content = $"""
                        [Desktop Entry]
                        Type=Application
                        Name={AppName}
                        Comment=Guild Wars 2 arcdps log uploader
                        Exec="{exePath}" -m
                        Terminal=false
                        Hidden=false
                        NoDisplay=false
                        X-GNOME-Autostart-enabled=true
                        Categories=Utility;Game;
                        """;
                    File.WriteAllText(desktopFile, content);
                }
                else if (File.Exists(desktopFile))
                {
                    File.Delete(desktopFile);
                }
            }
        }
        catch
        {
            // Ignore OS security or permission exceptions
        }
    }

    public static void ResetStartup()
    {
        SetStartup(false);
    }
}
