namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="MassDeletionSuspected"/> values.</summary>
public static class MassDeletionSuspectedFactory
{
    /// <summary>Creates a hold entry. Negative counts are treated as zero.</summary>
    /// <param name="pairId">The pair whose deletions are held.</param>
    /// <param name="missing">The missing baseline items.</param>
    /// <param name="total">The baseline items of the pair.</param>
    public static MassDeletionSuspected Create(SyncPairId pairId, int missing, int total) => new(pairId, Math.Max(missing, 0), Math.Max(total, 0));
}
