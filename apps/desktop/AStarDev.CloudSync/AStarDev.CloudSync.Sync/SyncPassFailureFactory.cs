namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncPassFailure"/> values.</summary>
public static class SyncPassFailureFactory
{
    /// <summary>Creates the <see cref="SyncPassFailure.AlreadyRunning"/> case.</summary>
    public static SyncPassFailure CreateAlreadyRunning() => new SyncPassFailure.AlreadyRunning();

    /// <summary>Creates the <see cref="SyncPassFailure.AuthenticationRequired"/> case.</summary>
    public static SyncPassFailure CreateAuthenticationRequired() => new SyncPassFailure.AuthenticationRequired();

    /// <summary>Creates the <see cref="SyncPassFailure.RootUnavailable"/> case.</summary>
    public static SyncPassFailure CreateRootUnavailable() => new SyncPassFailure.RootUnavailable();

    /// <summary>Creates the <see cref="SyncPassFailure.QuotaExceeded"/> case.</summary>
    public static SyncPassFailure CreateQuotaExceeded() => new SyncPassFailure.QuotaExceeded();

    /// <summary>Creates the <see cref="SyncPassFailure.InsufficientDiskSpace"/> case.</summary>
    public static SyncPassFailure CreateInsufficientDiskSpace() => new SyncPassFailure.InsufficientDiskSpace();

    /// <summary>Creates the <see cref="SyncPassFailure.Cancelled"/> case.</summary>
    public static SyncPassFailure CreateCancelled() => new SyncPassFailure.Cancelled();

    /// <summary>Creates the <see cref="SyncPassFailure.Unexpected"/> case.</summary>
    public static SyncPassFailure CreateUnexpected() => new SyncPassFailure.Unexpected();
}
