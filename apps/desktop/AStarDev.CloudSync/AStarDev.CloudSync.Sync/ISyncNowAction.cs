using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>The user's Sync Now for one account (SE-03, #1402).</summary>
public interface ISyncNowAction
{
    /// <summary>Starts a manual sync run off the calling thread and completes when it ends. When a run is already in progress the user is asked first (SE-08): "cancel and start again" cancels the running sync, waits for it to end, then starts a new run; "keep" changes nothing. The account's status shows how the run ended.</summary>
    /// <param name="accountId">The account to sync.</param>
    /// <param name="cancellationToken">Cancels the request and the run it started.</param>
    Task SyncNowAsync(AccountId accountId, CancellationToken cancellationToken);
}
