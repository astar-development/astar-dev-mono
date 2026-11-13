using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>One entry of the "needs attention" queue (#1405): a conflict, or deletions the mass-deletion guard holds.</summary>
public abstract record SyncAttentionItem
{
    private SyncAttentionItem()
    {
    }

    /// <summary>A path whose two sides cannot be reconciled without the user.</summary>
    public sealed record Conflict : SyncAttentionItem
    {
        internal Conflict(ConflictId id, AccountId accountId, RelativeSyncPath path, ConflictKind kind, SideSnapshot local, SideSnapshot remote, DateTimeOffset detectedAt)
        {
            Id = id;
            AccountId = accountId;
            Path = path;
            Kind = kind;
            Local = local;
            Remote = remote;
            DetectedAt = detectedAt;
        }

        /// <summary>The conflict's id.</summary>
        public ConflictId Id { get; }

        /// <summary>The account.</summary>
        public AccountId AccountId { get; }

        /// <summary>The conflicted path, relative to the sync root.</summary>
        public RelativeSyncPath Path { get; }

        /// <summary>Why it is a conflict.</summary>
        public ConflictKind Kind { get; }

        /// <summary>The local side when it was detected.</summary>
        public SideSnapshot Local { get; }

        /// <summary>The cloud side when it was detected.</summary>
        public SideSnapshot Remote { get; }

        /// <summary>When it was first detected.</summary>
        public DateTimeOffset DetectedAt { get; }

        /// <summary>Returns a redacted form, so paths never reach logs by accident.</summary>
        public override string ToString() => "SyncAttentionItem.Conflict(redacted)";
    }

    /// <summary>Deletions of one pair and direction that the mass-deletion guard holds until the user confirms or restores them.</summary>
    public sealed record HeldDeletions : SyncAttentionItem
    {
        internal HeldDeletions(HeldDeletionId id, AccountId accountId, SyncPairId pairId, DeletionDirection direction, IReadOnlyList<RelativeSyncPath> paths, DateTimeOffset detectedAt)
        {
            Id = id;
            AccountId = accountId;
            PairId = pairId;
            Direction = direction;
            Paths = paths;
            DetectedAt = detectedAt;
        }

        /// <summary>The held set's id.</summary>
        public HeldDeletionId Id { get; }

        /// <summary>The account.</summary>
        public AccountId AccountId { get; }

        /// <summary>The sync pair the deletions belong to.</summary>
        public SyncPairId PairId { get; }

        /// <summary>Which side the deletions would be applied to.</summary>
        public DeletionDirection Direction { get; }

        /// <summary>The held paths, sorted ignoring case, with no duplicates.</summary>
        public IReadOnlyList<RelativeSyncPath> Paths { get; }

        /// <summary>When the hold was first recorded.</summary>
        public DateTimeOffset DetectedAt { get; }

        /// <summary>Returns a redacted form, so paths never reach logs by accident.</summary>
        public override string ToString() => "SyncAttentionItem.HeldDeletions(redacted)";
    }
}
