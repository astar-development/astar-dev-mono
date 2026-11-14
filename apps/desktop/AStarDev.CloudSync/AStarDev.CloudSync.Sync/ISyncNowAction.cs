using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Starts a manual sync of one account (#1402). Different accounts can sync at the same time.</summary>
public interface ISyncNowAction
{
    /// <summary>Runs a manual sync of the account off the calling thread. When a run is already in progress it asks the user first (SE-08): "cancel and start again" cancels the run and starts a manual one, "keep" changes nothing. How the run ended is shown through the account's status, and is not returned.</summary>
    /// <param name="accountId">The account to sync.</param>
    /// <param name="cancellationToken">Cancels the run this call started.</param>
    /// <returns>A task that completes when the run this call started has ended, or straight away when the user kept the current run.</returns>
    Task SyncNowAsync(AccountId accountId, CancellationToken cancellationToken);
}
