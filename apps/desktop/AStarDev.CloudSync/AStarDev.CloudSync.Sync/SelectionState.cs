namespace AStarDev.CloudSync.Sync;

/// <summary>What the selection rules say about a folder (#1397). Sync status is separate and comes from the engine.</summary>
public abstract record SelectionState
{
    private SelectionState()
    {
    }

    /// <summary>The folder syncs. It belongs to the pair whose top-most Include rule covers it.</summary>
    public sealed record Included : SelectionState
    {
        internal Included(SyncPairId pair, SyncDirection direction)
        {
            Pair = pair;
            Direction = direction;
        }

        /// <summary>The Pair.</summary>
        public SyncPairId Pair { get; }

        /// <summary>The Direction.</summary>
        public SyncDirection Direction { get; }
    }

    /// <summary>The folder does not sync because a rule excludes it.</summary>
    public sealed record Excluded : SelectionState
    {
        internal Excluded()
        {
        }
    }

    /// <summary>No rule covers the folder, so it does not sync yet.</summary>
    public sealed record Uncovered : SelectionState
    {
        internal Uncovered()
        {
        }
    }

    /// <summary>The folder's own state differs from at least one stored rule below it.</summary>
    public sealed record PartiallyIncluded : SelectionState
    {
        internal PartiallyIncluded()
        {
        }
    }
}
