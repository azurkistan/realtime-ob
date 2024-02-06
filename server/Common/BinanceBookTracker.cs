using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Serilog;

public sealed class BinanceBookTracker(string symbol, BinanceApi bapi) : IDisposable
{
    public event Func<string, Depth, ValueTask> OnTick = static (_, _) => default;

    private readonly JsonSerializerOptions? _serializerOption = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private Depth _depth = new();

    readonly Channel<DepthUpdateEvent> _eventChannel = Channel.CreateUnbounded<DepthUpdateEvent>(new()
    {
        SingleReader = true,
        SingleWriter = true,
    });

    private long startingTimestamp = Stopwatch.GetTimestamp();

    public async Task Start(CancellationToken cancellationToken)
    {
        _ = StartWebsocketReceive(cancellationToken);

        try
        {
            // wait for at least 1 event to be received
            await _eventChannel.Reader.WaitToReadAsync(cancellationToken);


            // get initial snapshot
            using var http = new HttpClient();
            _depth = await bapi.GetDepthAsync(symbol, cancellationToken);
            Log.Information("Received snapshot for {Symbol}", symbol);
            // todo handle error

            // ignore all events which are already included in the snapshot
            DepthUpdateEvent ev;
            do
            {
                ev = await _eventChannel.Reader.ReadAsync(cancellationToken);
            } while (ev.LastUpdateId <= _depth.LastUpdateId);

            // todo keep checking if the events are being received in orderly manner

            while (!cancellationToken.IsCancellationRequested)
            {
                await OnDepthUpdate(ev);
                ev = await _eventChannel.Reader.ReadAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in start method");
        }
    }


    private async Task StartWebsocketReceive(CancellationToken cancellationToken)
    {
        var buf = ArrayPool<byte>.Shared.Rent(1024 * 1024);
        try
        {
            var lowercaseSymbol = symbol.ToLowerInvariant();
            Log.Information("Weboscket starting");
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri($"wss://stream.binance.com:9443/ws/{lowercaseSymbol}@depth@100ms"),
                cancellationToken);

            var subRequestString = JsonSerializer.SerializeToUtf8Bytes(new WsSubscriptionModel()
            {
                Method = "SUBSCRIBE",
                Params = [$"{lowercaseSymbol}@depth"],
                Id = 1
            });
            Log.Information("Subscribing");

            await ws.SendAsync(subRequestString,
                WebSocketMessageType.Text,
                true,
                cancellationToken);

            // ignore subscription confirmation reply
            // var confRec = await ws.ReceiveAsync(buf, cancellationToken);

            // Log.Information("receive conf: {Text}", Encoding.UTF8.GetString(buf));

            startingTimestamp = Stopwatch.GetTimestamp();
            while (!cancellationToken.IsCancellationRequested)
            {
                var rec = await ws.ReceiveAsync(buf, cancellationToken);

                if (rec.Count == 0 || rec.CloseStatus is not null)
                {
                    Log.Information("No websocket message");
                    continue;
                }

                var update = JsonSerializer.Deserialize<DepthUpdateEvent>(buf.AsSpan(0, rec.Count), _serializerOption);
                if (update is null)
                {
                    Log.Warning("Error parsing websocket update for {Symbol}", symbol);
                    continue;
                }

                var elapsedTime = Stopwatch.GetElapsedTime(startingTimestamp);
                if (elapsedTime.TotalMilliseconds > 150)
                    Log.Information("Abnormal websocket delay: {Stopwatch:F3}ms", elapsedTime.TotalMilliseconds);

                await _eventChannel.Writer.WriteAsync(update, cancellationToken);
                startingTimestamp = Stopwatch.GetTimestamp();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error ocurred while receiving websocket data");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf, true);
        }
    }

    public async Task OnDepthUpdate(DepthUpdateEvent depthUpdate)
    {
        UpdateSnapshot(depthUpdate);

        await OnTick(symbol, _depth);
    }

    private void UpdateSnapshot(DepthUpdateEvent update)
    {
        DepthUpdate(_depth.Bids, update.BidUpdates);
        DepthUpdate(_depth.Asks, update.Asks);
    }

    private static void DepthUpdate(List<decimal[]> depthSide, List<decimal[]> updates)
    {
        foreach (var update in updates)
        {
            var index = depthSide.FindIndex(x => x[0] == update[0]);

            if (index != -1)
            {
                if (update[1] is 0)
                {
                    depthSide.RemoveAt(index);
                }
                else
                {
                    depthSide[index] =
                    [
                        ..update
                    ];
                }
            }
            else if (update[1] != 0)
            {
                depthSide.Add(update);
            }
        }
    }

    public void Dispose()
    {
        OnTick = null!;
        _depth = null!;
    }
}