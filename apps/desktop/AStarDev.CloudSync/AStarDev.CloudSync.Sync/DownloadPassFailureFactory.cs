namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadPassFailure"/> values.</summary>
public static class DownloadPassFailureFactory
{
    /// <summary>Creates the <see cref="DownloadPassFailure.RootUnavailable"/> case.</summary>
    public static DownloadPassFailure CreateRootUnavailable() => new DownloadPassFailure.RootUnavailable();

    /// <summary>Creates the <see cref="DownloadPassFailure.InsufficientDiskSpace"/> case.</summary>
    public static DownloadPassFailure CreateInsufficientDiskSpace() => new DownloadPassFailure.InsufficientDiskSpace();

    /// <summary>Creates the <see cref="DownloadPassFailure.AuthenticationRequired"/> case.</summary>
    public static DownloadPassFailure CreateAuthenticationRequired() => new DownloadPassFailure.AuthenticationRequired();

    /// <summary>Creates the <see cref="DownloadPassFailure.Unexpected"/> case.</summary>
    public static DownloadPassFailure CreateUnexpected() => new DownloadPassFailure.Unexpected();
}
