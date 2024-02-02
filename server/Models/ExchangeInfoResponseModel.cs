using System.Text.Json.Serialization;

public sealed class ExchangeInfoResponseModel
{
    [JsonPropertyName("symbols")]
    public SymbolModel[] Symbols { get; init; }
}