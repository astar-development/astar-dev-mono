namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncFailureReason"/> values.</summary>
public static class SyncFailureReasonFactory
{
    /// <summary>Creates the <see cref="SyncFailureReason.RootUnavailable"/> case.</summary>
    public static SyncFailureReason CreateRootUnavailable() => new SyncFailureReason.RootUnavailable();

    /// <summary>Creates the <see cref="SyncFailureReason.QuotaExceeded"/> case.</summary>
    public static SyncFailureReason CreateQuotaExceeded() => new SyncFailureReason.QuotaExceeded();

    /// <summary>Creates the <see cref="SyncFailureReason.InsufficientDiskSpace"/> case.</summary>
    public static SyncFailureReason CreateInsufficientDiskSpace() => new SyncFailureReason.InsufficientDiskSpace();

    /// <summary>Creates the <see cref="SyncFailureReason.Unexpected"/> case.</summary>
    public static SyncFailureReason CreateUnexpected() => new SyncFailureReason.Unexpected();
}
