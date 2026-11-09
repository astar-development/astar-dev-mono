namespace AStarDev.CloudSync.Sync;

/// <summary>What a selection rule says about a folder and everything under it, until a more specific rule says otherwise (#1397).</summary>
public abstract record SelectionRuleKind
{
    private SelectionRuleKind()
    {
    }

    /// <summary>The folder syncs in the given direction.</summary>
    public sealed record Include : SelectionRuleKind
    {
        internal Include(SyncDirection direction)
        {
            Direction = direction;
        }

        /// <summary>The Direction.</summary>
        public SyncDirection Direction { get; }
    }

    /// <summary>The folder does not sync. Its local copies are kept.</summary>
    public sealed record Exclude : SelectionRuleKind
    {
        internal Exclude()
        {
        }
    }
}
