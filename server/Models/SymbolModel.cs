using System.Text.Json.Serialization;

public sealed class SymbolModel
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; init; }
}