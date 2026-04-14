namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <summary>
/// Bootstraps the application shell after services are ready: restores persisted accounts,
/// distributes them to child view models, and activates the previously-active account.
/// </summary>
public interface IApplicationInitializer
{
    /// <summary>Runs all startup steps required before the main window is usable.</summary>
    Task InitializeAsync(CancellationToken ct = default);
}
