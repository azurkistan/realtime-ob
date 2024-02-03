using Serilog;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

services.AddSignalR();
services.AddCors();
services.AddMemoryCache();
services.AddHttpClient();

services.AddSingleton<BinanceApi>();
services.AddSingleton<Librarian>();
services.AddHostedService<LibrarianMaid>();

var app = builder.Build();

app.UseRouting();

// ignore cors
app.UseCors(cpb =>
{
    cpb.AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true);
});

app.MapHub<OrderbookHub>("/ws");


app.Map("/symbols",
    async (Librarian api, string name) =>
    {
        var symbols = await api.GetValidSymbolsAsync(name);

        return symbols;
    });

app.Map("/symbol",
    async (Librarian api, string name) =>
    {
        var symbol = await api.GetSymbolAsync(name);
        if (symbol is null)
            return null;

        return new
        {
            symbol = symbol.Symbol,
            tickSize = symbol.GetTickSize(),
        };
    });

app.Run();