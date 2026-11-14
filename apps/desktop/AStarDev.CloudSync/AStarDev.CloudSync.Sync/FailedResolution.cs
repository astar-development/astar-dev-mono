namespace AStarDev.CloudSync.Sync;

/// <summary>One conflict that was not resolved, and why.</summary>
public sealed record FailedResolution
{
    internal FailedResolution(ConflictId id, ResolutionFailure reason)
    {
        Id = id;
        Reason = reason;
    }

    /// <summary>The conflict.</summary>
    public ConflictId Id { get; }

    /// <summary>Why it was not resolved.</summary>
    public ResolutionFailure Reason { get; }
}
