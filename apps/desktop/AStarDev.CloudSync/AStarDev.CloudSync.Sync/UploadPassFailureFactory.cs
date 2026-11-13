namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadPassFailure"/> cases.</summary>
public static class UploadPassFailureFactory
{
    /// <summary>Creates the <see cref="UploadPassFailure.RootUnavailable"/> case.</summary>
    public static UploadPassFailure CreateRootUnavailable() => new UploadPassFailure.RootUnavailable();

    /// <summary>Creates the <see cref="UploadPassFailure.QuotaExceeded"/> case.</summary>
    public static UploadPassFailure CreateQuotaExceeded() => new UploadPassFailure.QuotaExceeded();

    /// <summary>Creates the <see cref="UploadPassFailure.AuthenticationRequired"/> case.</summary>
    public static UploadPassFailure CreateAuthenticationRequired() => new UploadPassFailure.AuthenticationRequired();

    /// <summary>Creates the <see cref="UploadPassFailure.Unexpected"/> case.</summary>
    public static UploadPassFailure CreateUnexpected() => new UploadPassFailure.Unexpected();
}
