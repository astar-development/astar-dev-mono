namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncStatus"/> cases.</summary>
public static class SyncStatusFactory
{
    /// <summary>Creates a syncing status. The percentage is clamped to 0–100.</summary>
    public static SyncStatus CreateSyncing(int percentComplete) => new SyncStatus.Syncing(Math.Clamp(percentComplete, 0, 100));

    /// <summary>Creates an up-to-date status.</summary>
    public static SyncStatus CreateUpToDate() => new SyncStatus.UpToDate();

    /// <summary>Creates a conflicts status. A count below 1 is treated as 1.</summary>
    public static SyncStatus CreateHasConflicts(int count) => new SyncStatus.HasConflicts(Math.Max(count, 1));

    /// <summary>Creates a paused status.</summary>
    public static SyncStatus CreatePaused() => new SyncStatus.Paused();

    /// <summary>Creates a failed status.</summary>
    public static SyncStatus CreateFailed(SyncFailureReason reason) => new SyncStatus.Failed(reason);

    /// <summary>Creates a cancelled status.</summary>
    public static SyncStatus CreateCancelled() => new SyncStatus.Cancelled();

    /// <summary>Creates a needs-attention status. Counts below 0 are treated as 0.</summary>
    public static SyncStatus CreateNeedsAttention(int conflicts, int heldDeletions) => new SyncStatus.NeedsAttention(Math.Max(conflicts, 0), Math.Max(heldDeletions, 0));
}
