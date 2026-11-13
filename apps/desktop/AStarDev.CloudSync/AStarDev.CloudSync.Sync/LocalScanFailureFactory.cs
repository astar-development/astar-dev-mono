using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalScanFailure"/> cases.</summary>
public static class LocalScanFailureFactory
{
    /// <summary>Creates the <see cref="LocalScanFailure.RootUnavailable"/> case.</summary>
    public static LocalScanFailure CreateRootUnavailable() => new LocalScanFailure.RootUnavailable();

    /// <summary>Creates the <see cref="LocalScanFailure.Unreadable"/> case.</summary>
    public static LocalScanFailure CreateUnreadable() => new LocalScanFailure.Unreadable();

    /// <summary>Creates the <see cref="LocalScanFailure.Unexpected"/> case.</summary>
    /// <param name="error">The error.</param>
    public static LocalScanFailure CreateUnexpected(ErrorResponse error) => new LocalScanFailure.Unexpected(error);
}
