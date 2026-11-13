namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="HeldLocalDeletions"/> values.</summary>
public static class HeldLocalDeletionsFactory
{
    /// <summary>Creates the held deletions of a pair. Paths are de-duplicated ignoring case.</summary>
    /// <param name="pairId">The pair.</param>
    /// <param name="paths">The held paths.</param>
    /// <param name="total">How many baseline items the pair has.</param>
    /// <returns>The held deletions.</returns>
    public static HeldLocalDeletions Create(SyncPairId pairId, IEnumerable<RelativeSyncPath> paths, int total) => new(pairId, [.. (paths ?? []).Distinct()], Math.Max(total, 0));
}
