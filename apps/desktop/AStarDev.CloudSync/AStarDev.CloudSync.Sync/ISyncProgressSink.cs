using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Takes the progress the passes report while a run is in progress.</summary>
internal interface ISyncProgressSink
{
    /// <summary>Offers progress to the feed. The feed may hold it back to keep to its update rate.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="progress">The progress.</param>
    void Report(AccountId accountId, SyncProgress progress);
}
