using Hardstuck.Http;
using Newtonsoft.Json;
using PlenBotLogUploader.AppSettings;
using PlenBotLogUploader.DpsReport;
using PlenBotLogUploader.Teams;
using PlenBotLogUploader.Tools;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PlenBotLogUploader.Aleeva;

[JsonObject(MemberSerialization.OptIn)]
public class AleevaIntegration : IListViewItemInfo<AleevaIntegration>
{
    private Team _team;

    [JsonProperty("active")]
    public bool Active { get; set; } = true;

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("channel")]
    public string Channel { get; set; } = "";

    [JsonProperty("sendNotification")]
    public bool SendNotification { get; set; }

    [JsonProperty("sendOnSuccessOnly")]
    public bool SendOnSuccessOnly { get; set; }

    [JsonProperty("server")]
    public string Server { get; set; } = "";

    [JsonProperty("teamId")]
    public int TeamId { get; set; }

    /// <summary>
    ///     A selected integration team, with which the Aleeva integration should evaluate itself
    /// </summary>
    public Team Team
    {
        get
        {
            if (_team is null && Teams.Teams.All.TryGetValue(TeamId, out var team))
            {
                _team = team;
            }
            return _team;
        }
        set
        {
            _team = value;
            TeamId = value.Id;
        }
    }

    internal bool Valid => !string.IsNullOrWhiteSpace(Server) && !string.IsNullOrWhiteSpace(Channel);

    string IListViewItemInfo<AleevaIntegration>.NameToDisplay => Name;

    string IListViewItemInfo<AleevaIntegration>.TextToDisplay => !string.IsNullOrWhiteSpace(Name) ? Name : !string.IsNullOrWhiteSpace(Channel) ? $"C{Channel}" : $"S{Server}";

    bool IListViewItemInfo<AleevaIntegration>.CheckedToDisplay => Active;

    internal async Task PostLogToAleeva(ILogUploaderController mainLink, HttpClientController controller, DpsReportJson reportJson, List<LogPlayer> players)
    {
        if (!ApplicationSettings.Current.Aleeva.Authorised)
        {
            return;
        }
        if ((SendOnSuccessOnly && !(reportJson.Encounter.Success ?? false))
            || !(Team?.IsSatisfied(players) ?? false))
        {
            return;
        }
        try
        {
            var logObject = new AleevaAddReport
            {
                DpsReportPermalink = reportJson.ConfigAwarePermalink,
                SendNotification = SendNotification,
            };
            if (SendNotification)
            {
                logObject.NotificationServerId = Server;
                logObject.NotificationChannelId = Channel;
            }
            var jsonLogObject = JsonConvert.SerializeObject(logObject);
            using var content = new StringContent(jsonLogObject, Encoding.UTF8, "application/json");
            await controller.PostAsync($"{AleevaStatics.ApiAleevaUrl}/report", content);
        }
        catch
        {
            // do nothing
        }
    }
}
