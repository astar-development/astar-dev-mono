namespace AStarDev.CloudSync.Sync;

/// <summary>The local deletions of one sync pair that the mass-deletion guard holds back until the user confirms them (#1400).</summary>
public sealed record HeldLocalDeletions
{
    internal HeldLocalDeletions(SyncPairId pairId, IReadOnlyList<RelativeSyncPath> paths, int total)
    {
        PairId = pairId;
        Paths = paths;
        Total = total;
    }

    /// <summary>The pair whose deletions are held.</summary>
    public SyncPairId PairId { get; }

    /// <summary>The paths that would have been deleted locally, in plan order.</summary>
    public IReadOnlyList<RelativeSyncPath> Paths { get; }

    /// <summary>How many baseline items the pair has.</summary>
    public int Total { get; }
}
