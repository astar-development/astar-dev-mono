using AStar.Dev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

public interface IStartupService
{
    /// <summary>
    /// Loads all persisted accounts from the database.
    /// Returns them in display order with the previously-active account flagged.
    /// Does NOT attempt any network calls.
    /// </summary>
    Task<Result<List<OneDriveAccount>, string>> RestoreAccountsAsync();
}
