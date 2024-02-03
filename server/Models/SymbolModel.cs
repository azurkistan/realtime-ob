using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class SymbolModel
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }
    
    [JsonPropertyName("filters")]
    public JsonElement[] Filters { get; init; }

    private bool Equals(SymbolModel other)
        => Symbol == other.Symbol;


    public decimal GetTickSize()
    {
        foreach (var filter in Filters)
        {
            if (filter.GetProperty("filterType").GetString() == "PRICE_FILTER")
            {
                return decimal.Parse(filter.GetProperty("tickSize").GetString()!);
            }
        }

        return 0.001m;
    }
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || obj is SymbolModel other && Equals(other);

    public override int GetHashCode()
        => Symbol.GetHashCode();
}