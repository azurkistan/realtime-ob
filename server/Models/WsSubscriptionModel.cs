using System.Text.Json.Serialization;

public sealed class WsSubscriptionModel()
{
    [JsonPropertyName("method")]
    public string Method { get; set; }

    [JsonPropertyName("params")]
    public string[] Params { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }
}