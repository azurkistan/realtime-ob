using System.Text.Json.Serialization;

public sealed class DepthUpdateEvent
{
    [JsonPropertyName("e")]
    public string EventType { get; init; }

    [JsonPropertyName("E")]
    public long EventTime { get; init; }

    [JsonPropertyName("s")]
    public string Symbol { get; init; }

    [JsonPropertyName("U")]
    public long FirstUpdateId { get; init; }

    [JsonPropertyName("u")]
    public long LastUpdateId { get; init; }

    [JsonPropertyName("b")]
    public List<decimal[]> BidUpdates { get; init; }

    [JsonPropertyName("a")]
    public List<decimal[]> Asks { get; init; }
}