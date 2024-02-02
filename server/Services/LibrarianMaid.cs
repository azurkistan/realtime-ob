/// <summary>
/// Triggers cleaning on the librarian every 1 hour
/// </summary>
public sealed class LibrarianMaid(Librarian lib) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // todo increase this to 5 or more
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            await lib.Clean();
        }
    }
}