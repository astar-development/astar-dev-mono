namespace AStarDev.CloudSync.Sync;

/// <summary>Whether a sync pass may change a path. Only <see cref="Clear"/> allows a delete, a repair download or an upload.</summary>
public abstract record PathQuarantineState
{
    private PathQuarantineState()
    {
    }

    /// <summary>Nothing holds the path back.</summary>
    public sealed record Clear : PathQuarantineState
    {
        internal Clear()
        {
        }
    }

    /// <summary>The path has an unresolved conflict in the attention queue (#1405).</summary>
    public sealed record Conflicted : PathQuarantineState
    {
        internal Conflicted()
        {
        }
    }

    /// <summary>The mass-deletion guard is holding the path's deletion until the user confirms or restores it.</summary>
    public sealed record HeldByGuard : PathQuarantineState
    {
        internal HeldByGuard()
        {
        }
    }

    /// <summary>The path is never synced, on either side, for example a dot-prefixed folder or the app's own temp download file.</summary>
    public sealed record Skipped : PathQuarantineState
    {
        internal Skipped()
        {
        }
    }

    /// <summary>A full resync is running, so a missing item is not yet known to be deleted.</summary>
    public sealed record ResyncInProgress : PathQuarantineState
    {
        internal ResyncInProgress()
        {
        }
    }
}
