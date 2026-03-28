namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public interface IStartupTask
{
    string Name { get; }
    Task RunAsync(CancellationToken ct);
}
