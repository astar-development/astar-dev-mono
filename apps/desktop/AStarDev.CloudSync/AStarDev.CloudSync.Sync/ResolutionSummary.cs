namespace AStarDev.CloudSync.Sync;

/// <summary>What one resolution call did (#1406).</summary>
public sealed record ResolutionSummary
{
    internal ResolutionSummary(int resolved, IReadOnlyList<FailedResolution> failed, int skipped)
    {
        Resolved = resolved;
        Failed = failed;
        Skipped = skipped;
    }

    /// <summary>How many conflicts were settled and closed, cascaded ones included.</summary>
    public int Resolved { get; }

    /// <summary>The conflicts that stay open, each with its reason.</summary>
    public IReadOnlyList<FailedResolution> Failed { get; }

    /// <summary>How many conflicts were left in the queue on purpose.</summary>
    public int Skipped { get; }
}
