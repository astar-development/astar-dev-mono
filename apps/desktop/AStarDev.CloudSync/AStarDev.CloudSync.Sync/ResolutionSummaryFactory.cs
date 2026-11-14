namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ResolutionSummary"/> values.</summary>
public static class ResolutionSummaryFactory
{
    /// <summary>Creates a summary. A negative count becomes zero and a missing list reads as empty.</summary>
    /// <param name="resolved">How many conflicts were settled.</param>
    /// <param name="failed">The conflicts that stay open.</param>
    /// <param name="skipped">How many were left in the queue on purpose.</param>
    public static ResolutionSummary Create(int resolved, IEnumerable<FailedResolution> failed, int skipped) => new(Math.Max(resolved, 0), [.. failed ?? []], Math.Max(skipped, 0));
}
