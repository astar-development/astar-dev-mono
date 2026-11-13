namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadFailure"/> values.</summary>
public static class DownloadFailureFactory
{
    /// <summary>Creates the <see cref="DownloadFailure.Provider"/> case.</summary>
    public static DownloadFailure CreateProvider(ProviderError error) => new DownloadFailure.Provider(error);

    /// <summary>Creates the <see cref="DownloadFailure.Local"/> case.</summary>
    public static DownloadFailure CreateLocal(LocalOperationFailure failure) => new DownloadFailure.Local(failure);
}
