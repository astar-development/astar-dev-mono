using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Core;

/// <summary>Something that happened during syncing.</summary>
/// <param name="AccountId">The account it happened on.</param>
/// <param name="SyncPairId">The sync pair it happened on, if any.</param>
/// <param name="Kind">What happened.</param>
/// <param name="OccurredAt">When it happened.</param>
public sealed record ActivityEntry(AccountId AccountId, Option<SyncPairId> SyncPairId, ActivityKind Kind, DateTimeOffset OccurredAt);
