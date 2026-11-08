using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ActivityEntry"/> values.</summary>
public static class ActivityEntryFactory
{
    /// <summary>Creates an activity entry.</summary>
    public static ActivityEntry Create(AccountId accountId, Option<SyncPairId> syncPairId, ActivityKind kind, DateTimeOffset occurredAt) => new(accountId, syncPairId, kind, occurredAt);
}
