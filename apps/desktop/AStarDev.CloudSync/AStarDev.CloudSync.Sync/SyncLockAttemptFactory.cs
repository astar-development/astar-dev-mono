namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncLockAttempt"/> cases.</summary>
public static class SyncLockAttemptFactory
{
    /// <summary>Creates the acquired case.</summary>
    public static SyncLockAttempt CreateAcquired(SyncLease lease) => new SyncLockAttempt.Acquired(lease);

    /// <summary>Creates the busy case.</summary>
    public static SyncLockAttempt CreateBusy(SyncPassId runningPass) => new SyncLockAttempt.Busy(runningPass);

    /// <summary>Creates the draining case.</summary>
    public static SyncLockAttempt CreateDraining() => new SyncLockAttempt.Draining();
}
