using Avalonia;
using PlenBotLogUploader.AppSettings;
using PlenBotLogUploader.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PlenBotLogUploader;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var currProcess = Process.GetCurrentProcess();
        var processName = currProcess.ProcessName;
        var currId = currProcess.Id;
        var otherProcesses = Process.GetProcessesByName(processName)
            .Where(x => x.Id != currId)
            .ToArray();

        var anotherInstanceRunning = otherProcesses.Length != 0;
        foreach (var p in otherProcesses)
        {
            p.Dispose();
        }

        var localDir = ApplicationSettings.LocalDir;
        ApplicationSettings.Load();

        // Process command line arguments
        if (args.Length >= 2)
        {
            if (args[0].Equals("-resetsettings", StringComparison.OrdinalIgnoreCase) ||
                (args.Length > 1 && args[1].Equals("-resetsettings", StringComparison.OrdinalIgnoreCase)))
            {
                StartupManager.ResetStartup();
                new ApplicationSettings().Save();
            }
            else if (args.Contains("-finishupdate", StringComparer.OrdinalIgnoreCase))
            {
                var updateExe = Path.Combine(localDir, "PlenBotLogUploader_Update.exe");
                if (File.Exists(updateExe))
                {
                    try { File.Delete(updateExe); } catch { }
                }
            }
        }

        // If another instance is running, exit unless doing an update
        if (anotherInstanceRunning && !args.Contains("-update", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("Another instance of PlenBot Log Uploader is already running.");
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
