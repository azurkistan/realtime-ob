using System.Text.Json.Serialization;

public sealed class Depth
{
    [JsonPropertyName("lastUpdateId")]
    public long LastUpdateId { get; init; }

    [JsonPropertyName("bids")]
    public List<decimal[]> Bids { get; init; } = new();

    [JsonPropertyName("asks")]
    public List<decimal[]> Asks { get; init; } = new();
}