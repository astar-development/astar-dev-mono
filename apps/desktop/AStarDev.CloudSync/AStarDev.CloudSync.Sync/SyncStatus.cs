namespace AStarDev.CloudSync.Sync;

/// <summary>The sync state of an account or sync pair.</summary>
public abstract record SyncStatus
{
    private SyncStatus()
    {
    }

    /// <summary>Syncing, with the percentage of the current pass completed.</summary>
    public sealed record Syncing : SyncStatus
    {
        internal Syncing(int percentComplete)
        {
            PercentComplete = percentComplete;
        }

        /// <summary>The percentage of the current pass completed.</summary>
        public int PercentComplete { get; }
    }

    /// <summary>Fully in sync.</summary>
    public sealed record UpToDate : SyncStatus
    {
        internal UpToDate()
        {
        }
    }

    /// <summary>In sync apart from conflicts that need the user's decision.</summary>
    public sealed record HasConflicts : SyncStatus
    {
        internal HasConflicts(int count)
        {
            Count = count;
        }

        /// <summary>The number of conflicts waiting for a decision.</summary>
        public int Count { get; }
    }

    /// <summary>Paused by the user.</summary>
    public sealed record Paused : SyncStatus
    {
        internal Paused()
        {
        }
    }

    /// <summary>The last run failed.</summary>
    public sealed record Failed : SyncStatus
    {
        internal Failed(SyncFailureReason reason)
        {
            Reason = reason;
        }

        /// <summary>Why the run failed.</summary>
        public SyncFailureReason Reason { get; }
    }

    /// <summary>The last run was cancelled by the user. Work already finished is kept.</summary>
    public sealed record Cancelled : SyncStatus
    {
        internal Cancelled()
        {
        }
    }

    /// <summary>The last run finished, but conflicts or held deletions wait for the user.</summary>
    public sealed record NeedsAttention : SyncStatus
    {
        internal NeedsAttention(int conflicts, int heldDeletions)
        {
            Conflicts = conflicts;
            HeldDeletions = heldDeletions;
        }

        /// <summary>The number of conflicts waiting for a decision.</summary>
        public int Conflicts { get; }

        /// <summary>The number of deletions held until the user confirms them.</summary>
        public int HeldDeletions { get; }
    }
}
