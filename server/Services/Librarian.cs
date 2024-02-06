using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

/// <summary>
/// Tracks who is subscribed to what and passes messages around
/// </summary>
/// <param name="hubContext">Hub</param>
/// <param name="cache">Cache</param>
/// <param name="bapi">Binance api</param>
public sealed class Librarian(IHubContext<OrderbookHub> hubContext, IMemoryCache cache, BinanceApi bapi)
{
    private Dictionary<string, String> _userSubscriptions = new();
    private Dictionary<String, (BinanceBookTracker, CancellationTokenSource)> _bookTrackers = new();

    SnapshotDto MapDepthToSnapshotDto(Depth d)
    {
        var bids = d.Bids.ConvertAll(
            x => new QuotePairDto()
            {
                Price = x[0],
                Quantity = x[1]
            });

        bids.Sort((b, a) => a.Price.CompareTo(b.Price));

        var asks = d.Asks.ConvertAll(
            x => new QuotePairDto()
            {
                Price = x[0],
                Quantity = x[1]
            });

        asks.Sort((b, a) => a.Price.CompareTo(b.Price));

        return new()
        {
            Bids = bids,
            Asks = asks,
        };
    }

    public async Task Unsubscribe(string userId)
    {
        // check if user had a subscription and which one it is

        await _subLock.WaitAsync();
        try
        {
            await UnsubscribeInternal(userId);
        }
        finally
        {
            _subLock.Release();
        }
    }

    private async Task UnsubscribeInternal(string userId)
    {
        if (_userSubscriptions.Remove(userId, out var symbol))
        {
            await hubContext.Groups.RemoveFromGroupAsync(userId, symbol);
            Log.Information("User unsubbed from {Symbol}", symbol);
        }
    }

    private readonly SemaphoreSlim _subLock = new(1, 1);

    public async Task<string?> SubscribeAsync(string userId, string inputSymbol)
    {
        var potentialSymbol = await GetFuzzySymbolAsync(inputSymbol);
        if (potentialSymbol is not string symbol)
        {
            Log.Information("User inputted invalid symbol");
            // Log.Information(String.Join(", ", await bapi.GetValidSymbols()));
            return null;
        }

        await _subLock.WaitAsync();
        try
        {
            await UnsubscribeInternal(userId);
            if (_userSubscriptions.TryAdd(userId, symbol))
            {
                if (_bookTrackers.ContainsKey(symbol))
                    return symbol;

                Log.Information("Instantiating new tracker for {Symbol}", symbol);
                var tracker = new BinanceBookTracker(symbol, bapi);
                tracker.OnTick += TrackerOnOnTick;
                var tokenSource = new CancellationTokenSource();
                _ = tracker.Start(tokenSource.Token);
                _bookTrackers.TryAdd(symbol, (tracker, tokenSource));
            }
        }
        finally
        {
            _subLock.Release();
        }

        return symbol;
    }


    public async Task Clean()
    {
        // have to make sure there are no sneaky subs during cleanup
        await _subLock.WaitAsync();
        try
        {
            var activeSubs = _userSubscriptions.Values.ToHashSet();
            foreach (var symbol in _bookTrackers.Keys.ToArray())
            {
                if (!activeSubs.Contains(symbol))
                {
                    Log.Information("Clearing {Symbol} tracker due to vacancy", symbol);
                    // nobody is subscribed to this one
                    if (_bookTrackers.Remove(symbol, out var val))
                    {
                        var (tracker, cancelSource) = val;
                        tracker.OnTick -= TrackerOnOnTick;
                        await cancelSource.CancelAsync();
                        tracker.Dispose();
                        cancelSource.Dispose();
                    }
                }
            }
        }
        finally
        {
            _subLock.Release();
        }
    }


    private async ValueTask TrackerOnOnTick(String symbolName, Depth arg)
    {
        var data = MapDepthToSnapshotDto(arg);
        // TODO
        // we shouldn't send directly because we might throttle the update.
        // Write to channel, and then have a dedicated loop for sending events
        // or let the tracker handle it, pass the context to it
        _ = hubContext.Clients.Group(symbolName).SendAsync("upd", data);
    }

    private async Task<string?> GetFuzzySymbolAsync(string input)
    {
        input = input.Trim().ToLowerInvariant();
        var validSymbols = await cache.GetOrCreateAsync("valid_symbols", ValidSymbolFactory);

        if (validSymbols is not null && validSymbols.Contains(new()
            {
                Symbol = input
            }))
            return input;

        return null;
    }

    public async Task<SymbolModel?> GetSymbolAsync(string input)
    {
        input = input.Trim().ToLowerInvariant();

        var validSymbols = await cache.GetOrCreateAsync("valid_symbols", ValidSymbolFactory);

        if (validSymbols is null)
            return null;

        return validSymbols.FirstOrDefault(x => x.Symbol == input);
    }

    public async Task<string[]> GetValidSymbolsAsync(string input)
    {
        input = input.Trim().ToLowerInvariant();
        var validSymbols = await cache.GetOrCreateAsync("valid_symbols", ValidSymbolFactory);

        if (validSymbols is null)
            return Array.Empty<string>();

        Log.Information("Sesnding");
        return validSymbols
            .Select(x => x.Symbol)
            .Where(x => x.StartsWith(input)).ToArray();
    }


    private async Task<IReadOnlyCollection<SymbolModel>> ValidSymbolFactory(ICacheEntry cacheEntry)
    {
        // once an hour update valid symbols
        cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

        var symbols = await bapi.GetValidSymbols();
        return symbols.ToList();
    }
}