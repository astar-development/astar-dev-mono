namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <summary>Orchestrates the application startup sequence.</summary>
public interface IAppBootstrapper
{
    /// <summary>Runs the full startup sequence, reporting progress via <paramref name="progress"/>.</summary>
    Task BootstrapAsync(IProgress<string> progress, CancellationToken ct = default);
}
