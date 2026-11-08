namespace AStarDev.CloudSync.Accounts;

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
}
