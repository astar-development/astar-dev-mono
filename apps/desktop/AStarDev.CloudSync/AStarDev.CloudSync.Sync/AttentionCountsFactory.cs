namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="AttentionCounts"/> values.</summary>
public static class AttentionCountsFactory
{
    /// <summary>Creates the counts. Negative values are treated as zero.</summary>
    /// <param name="conflicts">The open conflicts.</param>
    /// <param name="heldDeletionSets">The held sets.</param>
    /// <param name="heldDeletionPaths">The paths across all held sets.</param>
    public static AttentionCounts Create(int conflicts, int heldDeletionSets, int heldDeletionPaths) => new(Math.Max(conflicts, 0), Math.Max(heldDeletionSets, 0), Math.Max(heldDeletionPaths, 0));
}
