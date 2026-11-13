using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Cancels an account's running sync (#1402).</summary>
public interface ISyncCancelAction
{
    /// <summary>Cancels the account's run and waits until it has ended and released the account, so a new run can start straight after. Does nothing when no run is in progress.</summary>
    /// <param name="accountId">The account whose run to cancel.</param>
    /// <param name="cancellationToken">Stops waiting for the run to end; the run is still cancelled.</param>
    /// <returns>A task that completes when the run has ended.</returns>
    Task CancelAsync(AccountId accountId, CancellationToken cancellationToken);
}
