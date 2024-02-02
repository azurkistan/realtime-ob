using System.Text.Json.Serialization;

public class QuotePairDto
{
    [JsonPropertyName("p")]
    public decimal Price { get; set; }
    [JsonPropertyName("q")]
    public decimal Quantity { get; set; }
}