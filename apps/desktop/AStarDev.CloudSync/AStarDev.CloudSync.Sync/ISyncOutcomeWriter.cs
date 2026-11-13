using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Stores the status an account's last sync run left (#1402).</summary>
public interface ISyncOutcomeWriter
{
    /// <summary>Saves the account's status, replacing the one stored before. Only resting statuses are stored: up to date, needs attention and failed.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="status">The status to store.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    Task<Result<Unit, ErrorResponse>> SaveAsync(AccountId accountId, SyncStatus status, CancellationToken cancellationToken);
}
