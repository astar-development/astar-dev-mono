namespace AStarDev.CloudSync.Sync;

/// <summary>A fact a planner knows that the two snapshots and the baseline cannot show.</summary>
public abstract record ConflictSignal
{
    private ConflictSignal()
    {
    }

    /// <summary>The planner has no extra fact; the snapshots decide.</summary>
    public sealed record None : ConflictSignal
    {
        internal None()
        {
        }
    }

    /// <summary>A move's target path is already in use on the other side.</summary>
    public sealed record MoveTargetInUse : ConflictSignal
    {
        internal MoveTargetInUse()
        {
        }
    }

    /// <summary>The cloud rejected a transfer with 412, or the target changed before the move.</summary>
    public sealed record PreconditionFailed : ConflictSignal
    {
        internal PreconditionFailed()
        {
        }
    }
}
