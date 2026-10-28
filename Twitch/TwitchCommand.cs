using Newtonsoft.Json;
using PlenBotLogUploader.Tools;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PlenBotLogUploader.Twitch;

[JsonObject(MemberSerialization.OptIn)]
public class TwitchCommand : IListViewItemInfo<TwitchCommand>
{
    private string _command = "";
    private bool _isRegEx;
    private Regex _regex;

    [JsonProperty("enabled")]
    public bool Enabled { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("isRegEx")]
    public bool IsRegEx
    {
        get => _isRegEx;
        set
        {
            if (_isRegEx != value)
            {
                _isRegEx = value;
                _regex = null;
            }
        }
    }

    [JsonProperty("command")]
    public string Command
    {
        get => _command;
        set
        {
            if (_command != value && value is not null)
            {
                _command = value;
                _regex = null;
            }
        }
    }

    internal Regex Regex
    {
        get
        {
            if (!_isRegEx || string.IsNullOrWhiteSpace(_command))
            {
                return null;
            }
            return _regex ??= new Regex(_command, RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }
    }

    [JsonProperty("response")]
    public string Response { get; set; } = "";

    [JsonProperty("responseType")]
    public TwitchResponseType ResponseType { get; set; } = TwitchResponseType.Plain;

    string IListViewItemInfo<TwitchCommand>.NameToDisplay => Name;

    string IListViewItemInfo<TwitchCommand>.TextToDisplay => Name;

    bool IListViewItemInfo<TwitchCommand>.CheckedToDisplay => Enabled;

    internal string FormattedResponse(string receiver, Dictionary<string, string> valuesToChange)
    {
        var response = Response;
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }
        foreach (var kvp in valuesToChange)
        {
            response = response.Replace(kvp.Key, kvp.Value);
        }
        var prepend = "";
        if (ResponseType == TwitchResponseType.ReplyAt)
        {
            var builder = new StringBuilder();
            builder.Append('@').Append(receiver).Append(' ');
            prepend = builder.ToString();
        }
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }
        return prepend + response;
    }
}
