namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncFailureReason"/> values.</summary>
public static class SyncFailureReasonFactory
{
    /// <summary>Creates the <see cref="SyncFailureReason.CouldNotStart"/> case.</summary>
    public static SyncFailureReason CreateCouldNotStart() => new SyncFailureReason.CouldNotStart();

    /// <summary>Creates the <see cref="SyncFailureReason.AuthenticationRequired"/> case.</summary>
    public static SyncFailureReason CreateAuthenticationRequired() => new SyncFailureReason.AuthenticationRequired();

    /// <summary>Creates the <see cref="SyncFailureReason.RootUnavailable"/> case.</summary>
    public static SyncFailureReason CreateRootUnavailable() => new SyncFailureReason.RootUnavailable();

    /// <summary>Creates the <see cref="SyncFailureReason.QuotaExceeded"/> case.</summary>
    public static SyncFailureReason CreateQuotaExceeded() => new SyncFailureReason.QuotaExceeded();

    /// <summary>Creates the <see cref="SyncFailureReason.InsufficientDiskSpace"/> case.</summary>
    public static SyncFailureReason CreateInsufficientDiskSpace() => new SyncFailureReason.InsufficientDiskSpace();

    /// <summary>Creates the <see cref="SyncFailureReason.Cancelled"/> case.</summary>
    public static SyncFailureReason CreateCancelled() => new SyncFailureReason.Cancelled();

    /// <summary>Creates the <see cref="SyncFailureReason.Unexpected"/> case.</summary>
    public static SyncFailureReason CreateUnexpected() => new SyncFailureReason.Unexpected();
}
