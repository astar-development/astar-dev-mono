using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="InterruptedSyncRun"/> values.</summary>
public static class InterruptedSyncRunFactory
{
    /// <summary>Creates an interrupted run.</summary>
    /// <param name="id">The run.</param>
    /// <param name="accountId">The account the run was for.</param>
    public static InterruptedSyncRun Create(SyncRunId id, AccountId accountId) => new(id, accountId);
}
