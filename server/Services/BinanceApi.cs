using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using SymbolName = string;

public sealed class BinanceApi(IHttpClientFactory _httpClientFactory)
{
    private const string BASE_URL = "https://api.binance.com";

    private JsonSerializerOptions? _serializerOption = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public async Task<SymbolModel[]> GetValidSymbols()
    {
        using var client = _httpClientFactory.CreateClient();

        // only get leveraged tokens
        var res = await client.GetFromJsonAsync<ExchangeInfoResponseModel>($"{BASE_URL}/api/v3/exchangeInfo?permissions=[\"MARGIN\"]");
        if (res is null)
            throw new NullReferenceException("Invalid json received for all pairs api call");

        foreach (var s in res.Symbols)
            s.Symbol = s.Symbol.ToLowerInvariant();
        return res.Symbols;
    }

    public async Task<Depth> GetDepthAsync(SymbolName symbol, CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        return await httpClient.GetFromJsonAsync<Depth>(
                   $"{BASE_URL}/api/v3/depth?symbol={symbol.ToUpperInvariant()}&limit=1000",
                   options: _serializerOption,
                   cancellationToken: cancellationToken)
               ?? throw new NullReferenceException("Couldn't retrieve depth for this symbol");
    }
}