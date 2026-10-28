using Newtonsoft.Json;
using PlenBotLogUploader.AppSettings;
using PlenBotLogUploader.DpsReport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZLinq;

namespace PlenBotLogUploader.Tools;

public static class WingmanRePing
{
    private static string FileLocation => Path.Combine(ApplicationSettings.LocalDir, "wingmanreping.json");
    private static readonly object _lock = new();

    private static HashSet<WingmanPingInfo> _logsToPing;

    internal static HashSet<WingmanPingInfo> LogsToPing
    {
        get
        {
            lock (_lock)
            {
                if (_logsToPing is not null)
                {
                    return _logsToPing;
                }
                if (File.Exists(FileLocation))
                {
                    try
                    {
                        _logsToPing = JsonConvert.DeserializeObject<List<WingmanPingInfo>>(File.ReadAllText(FileLocation))
                                                              .AsValueEnumerable()
                                                              .Where(x => File.Exists(x.FilePath))
                                                              .GroupBy(x => x.FilePath)
                                                              .Select(x => x.AsValueEnumerable().First())
                                                              .ToHashSet();
                    }
                    catch
                    {
                        _logsToPing = [];
                    }
                }
                else
                {
                    _logsToPing = [];
                }
                return _logsToPing;
            }
        }
    }
    
    internal static void SaveLogsToPing()
    {
        lock (_lock)
        {
            try
            {
                File.WriteAllText(FileLocation, JsonConvert.SerializeObject(LogsToPing, Formatting.Indented));
            }
            catch
            {
                // do nothing
            }
        }
    }

    internal static void RemoveLogAndSave(string file)
    {
        lock (_lock)
        {
            var logToRemove = LogsToPing
                .AsValueEnumerable()
                .FirstOrDefault(x => file.Equals(x.FilePath, StringComparison.OrdinalIgnoreCase));
            if (logToRemove is null)
            {
                return;
            }
            if (LogsToPing.Remove(logToRemove))
            {
                SaveLogsToPing();
            }
        }
    }

    internal static void ProcessLogs(SemaphoreSlim semaphore, Func<string, DpsReportJsonExtraJson, Task> process)
    {
        List<WingmanPingInfo> items;
        lock (_lock)
        {
            LogsToPing.RemoveWhere(x => !File.Exists(x.FilePath));
            items = [.. LogsToPing];
        }

        foreach (var wingmanPingInfo in items)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var convertedExtraJson = new DpsReportJsonExtraJson
                    {
                        TriggerId = wingmanPingInfo.TriggerId,
                        RecordedByAccountName = wingmanPingInfo.UploadedBy,
                    };
                    await semaphore.WaitAsync();
                    try
                    {
                        await process(wingmanPingInfo.FilePath, convertedExtraJson);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
                catch
                {
                    // ignored
                }
            });
        }
    }
}
