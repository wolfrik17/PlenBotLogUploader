using Newtonsoft.Json;

namespace PlenBotLogUploader.AppSettings;

[JsonObject(MemberSerialization.OptIn)]
public sealed class ApplicationSettingsUploadUserToken
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("active")]
    public bool Active { get; set; }

    [JsonProperty("userToken")]
    public string UserToken { get; set; } = "";

    public override string ToString() => Name;
}
