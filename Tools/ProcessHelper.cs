using System;
using System.Diagnostics;
using System.IO;

namespace PlenBotLogUploader.Tools;

internal static class ProcessHelper
{
    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = url,
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", url);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open URL '{url}': {ex.Message}");
        }
    }

    public static void OpenDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = path,
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", path);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", path);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open directory '{path}': {ex.Message}");
        }
    }
}
