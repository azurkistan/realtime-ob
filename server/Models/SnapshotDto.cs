using System.Text.Json.Serialization;

public sealed class SnapshotDto
{
    [JsonPropertyName("b")]
    public List<QuotePairDto> Bids { get; init; }
    
    [JsonPropertyName("a")]
    public List<QuotePairDto> Asks { get; init; }
}