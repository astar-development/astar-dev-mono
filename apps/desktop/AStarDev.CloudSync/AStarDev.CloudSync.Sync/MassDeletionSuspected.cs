namespace AStarDev.CloudSync.Sync;

/// <summary>The mass-deletion guard held one pair's local deletions in a scan (#1399): too many of its baseline items were missing at once. The pair's other changes are still reported, and its deleted paths read as <see cref="PathQuarantineState.HeldByGuard"/> until the user confirms or restores them.</summary>
public sealed record MassDeletionSuspected
{
    internal MassDeletionSuspected(SyncPairId pairId, int missing, int total)
    {
        PairId = pairId;
        Missing = missing;
        Total = total;
    }

    /// <summary>The pair whose deletions are held.</summary>
    public SyncPairId PairId { get; }

    /// <summary>How many of the pair's baseline items are missing on disk.</summary>
    public int Missing { get; }

    /// <summary>How many baseline items the pair has.</summary>
    public int Total { get; }
}
