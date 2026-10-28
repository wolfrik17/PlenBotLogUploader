using PlenBotLogUploader.AppSettings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZLinq;

namespace PlenBotLogUploader.Tools;

internal static class LogReuploader
{
    private static string FileLocation => Path.Combine(ApplicationSettings.LocalDir, "faileduploads.txt");
    private static readonly object _lock = new();

    private static HashSet<string> _failedLogs;
    private static readonly Dictionary<string, string> PostData = new()
    {
        { "generator", "ei" },
        { "json", "1" },
    };

    internal static HashSet<string> FailedLogs
    {
        get
        {
            lock (_lock)
            {
                if (_failedLogs is not null)
                {
                    return _failedLogs;
                }
                if (File.Exists(FileLocation))
                {
                    try
                    {
                        _failedLogs = File.ReadAllLines(FileLocation)
                            .AsValueEnumerable()
                            .Where(File.Exists)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        _failedLogs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }
                }
                else
                {
                    _failedLogs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
                return _failedLogs;
            }
        }
    }

    internal static void SaveFailedLogs()
    {
        lock (_lock)
        {
            try
            {
                File.WriteAllLines(FileLocation, FailedLogs);
            }
            catch
            {
                // do nothing
            }
        }
    }

    internal static void AddLogAndSave(string file)
    {
        lock (_lock)
        {
            if (FailedLogs.Add(file))
            {
                SaveFailedLogs();
            }
        }
    }

    internal static bool RemoveLogAndSave(string file)
    {
        lock (_lock)
        {
            var removed = FailedLogs.Remove(file);
            if (removed)
            {
                SaveFailedLogs();
            }
            return removed;
        }
    }

    internal static void ProcessLogs(SemaphoreSlim semaphore, Func<string, Dictionary<string, string>, bool, Task> process)
    {
        List<string> logsToProcess;
        lock (_lock)
        {
            // Prune missing files safely
            FailedLogs.RemoveWhere(f => !File.Exists(f));
            logsToProcess = [.. FailedLogs];
        }

        foreach (var fileName in logsToProcess)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await process(fileName, PostData, true);
                }
                catch
                {
                    // ignored
                }
            });
        }
    }
}
