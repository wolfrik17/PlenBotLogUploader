using Newtonsoft.Json;
using PlenBotLogUploader.AppSettings;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PlenBotLogUploader.RemotePing;

internal static class PingConfigurations
{
    internal static string JsonFileLocation => Path.Combine(ApplicationSettings.LocalDir, "remote_pings.json");

    internal static Dictionary<int, PingConfiguration> All { get; private set; } = new();

    internal static Dictionary<int, PingConfiguration> LoadPings()
    {
        try
        {
            if (File.Exists(JsonFileLocation))
            {
                var jsonData = File.ReadAllText(JsonFileLocation);
                var remotePingId = 1;
                var parsedData = JsonConvert.DeserializeObject<IEnumerable<PingConfiguration>>(jsonData);
                if (parsedData is not null)
                {
                    All = parsedData.Select(x => (Key: remotePingId++, Config: x))
                        .ToDictionary(x => x.Key, x => x.Config);
                    return All;
                }
            }
        }
        catch
        {
            // do nothing
        }

        return All;
    }

    internal static void SaveToJson(IEnumerable<PingConfiguration> configurations)
    {
        try
        {
            var jsonString = JsonConvert.SerializeObject(configurations, Formatting.Indented);
            File.WriteAllText(JsonFileLocation, jsonString, Encoding.UTF8);
        }
        catch
        {
            // do nothing
        }
    }
}
