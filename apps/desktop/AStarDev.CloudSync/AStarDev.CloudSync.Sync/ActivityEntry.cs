using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Something that happened during syncing.</summary>
public sealed record ActivityEntry
{
    internal ActivityEntry(AccountId accountId, Option<SyncPairId> syncPairId, ActivityKind kind, DateTimeOffset occurredAt)
    {
        AccountId = accountId;
        SyncPairId = syncPairId;
        Kind = kind;
        OccurredAt = occurredAt;
    }

    /// <summary>The account it happened on.</summary>
    public AccountId AccountId { get; }

    /// <summary>The sync pair it happened on, if any.</summary>
    public Option<SyncPairId> SyncPairId { get; }

    /// <summary>What happened.</summary>
    public ActivityKind Kind { get; }

    /// <summary>When it happened.</summary>
    public DateTimeOffset OccurredAt { get; }
}
