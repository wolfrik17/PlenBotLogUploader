using System;
using System.IO;

namespace PlenBotLogUploader.Tools;

#nullable enable

internal static class ArcLogHelper
{
    /// <summary>
    /// Attempts to automatically discover the arcDPS combat logs directory on Windows or Linux.
    /// </summary>
    public static string? TryDetectArcLogsLocation()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (!string.IsNullOrEmpty(myDocs))
                {
                    var p1 = Path.Combine(myDocs, "Guild Wars 2", "addons", "arcdps", "arcdps.cbtlogs");
                    if (Directory.Exists(p1)) return p1;
                    var p2 = Path.Combine(myDocs, "Guild Wars 2", "addons", "arcdps");
                    if (Directory.Exists(p2)) return p2;
                }

                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(userProfile))
                {
                    var oneDrive = Path.Combine(userProfile, "OneDrive", "Documents", "Guild Wars 2", "addons", "arcdps", "arcdps.cbtlogs");
                    if (Directory.Exists(oneDrive)) return oneDrive;
                }
            }
            else
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrEmpty(home)) return null;

                // Standard Steam Proton locations
                var steamRoots = new[]
                {
                    Path.Combine(home, ".local", "share", "Steam", "steamapps", "compatdata"),
                    Path.Combine(home, ".steam", "steam", "steamapps", "compatdata"),
                    Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", "data", "Steam", "steamapps", "compatdata")
                };

                foreach (var steamRoot in steamRoots)
                {
                    if (!Directory.Exists(steamRoot)) continue;

                    // Standard GW2 Steam App ID: 1284210
                    var standardGw2 = Path.Combine(steamRoot, "1284210", "pfx", "drive_c", "users", "steamuser", "Documents", "Guild Wars 2", "addons", "arcdps", "arcdps.cbtlogs");
                    if (Directory.Exists(standardGw2)) return standardGw2;

                    var standardGw2Fallback = Path.Combine(steamRoot, "1284210", "pfx", "drive_c", "users", "steamuser", "Documents", "Guild Wars 2", "addons", "arcdps");
                    if (Directory.Exists(standardGw2Fallback)) return standardGw2Fallback;

                    // Non-Steam shortcut prefix probe
                    try
                    {
                        foreach (var prefixDir in Directory.EnumerateDirectories(steamRoot))
                        {
                            var candidate = Path.Combine(prefixDir, "pfx", "drive_c", "users", "steamuser", "Documents", "Guild Wars 2", "addons", "arcdps", "arcdps.cbtlogs");
                            if (Directory.Exists(candidate)) return candidate;
                        }
                    }
                    catch { }
                }

                // Lutris standard paths
                var lutrisGw2 = Path.Combine(home, "Games", "guild-wars-2", "drive_c");
                if (Directory.Exists(lutrisGw2))
                {
                    try
                    {
                        var usersDir = Path.Combine(lutrisGw2, "users");
                        if (Directory.Exists(usersDir))
                        {
                            foreach (var user in Directory.EnumerateDirectories(usersDir))
                            {
                                var candidate = Path.Combine(user, "Documents", "Guild Wars 2", "addons", "arcdps", "arcdps.cbtlogs");
                                if (Directory.Exists(candidate)) return candidate;
                            }
                        }
                    }
                    catch { }
                }

                // Bottles standard paths
                var bottlesRoot = Path.Combine(home, ".var", "app", "com.usebottles.bottles", "data", "bottles", "bottles");
                if (Directory.Exists(bottlesRoot))
                {
                    try
                    {
                        foreach (var bottle in Directory.EnumerateDirectories(bottlesRoot))
                        {
                            var usersDir = Path.Combine(bottle, "drive_c", "users");
                            if (Directory.Exists(usersDir))
                            {
                                foreach (var user in Directory.EnumerateDirectories(usersDir))
                                {
                                    var candidate = Path.Combine(user, "Documents", "Guild Wars 2", "addons", "arcdps", "arcdps.cbtlogs");
                                    if (Directory.Exists(candidate)) return candidate;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        return null;
    }
}
