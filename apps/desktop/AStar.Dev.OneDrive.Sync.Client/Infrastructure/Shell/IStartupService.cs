using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

public interface IStartupService
{
    /// <summary>
    /// Loads all persisted accounts from the database.
    /// Returns them in display order with the previously-active account flagged.
    /// Does NOT attempt any network calls.
    /// </summary>
    Task<List<OneDriveAccount>> RestoreAccountsAsync();
}

