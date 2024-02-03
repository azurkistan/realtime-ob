using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Serilog;

public sealed class OrderbookHub(Librarian l) : Hub
{
    private const string UPDATE = "upd";

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await l.Unsubscribe(Context.ConnectionId);
    }

    [HubMethodName("sub")]
    public async Task<bool> Sub(string symbolName)
    {
        var safeSymbol = await l.SubscribeAsync(Context.ConnectionId, symbolName);
        if (safeSymbol is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, symbolName);
            return true;
        }

        return false;
    }

    public static string GetUpdateMethod(string symbolName)
        => $"{UPDATE}/{symbolName}";
}