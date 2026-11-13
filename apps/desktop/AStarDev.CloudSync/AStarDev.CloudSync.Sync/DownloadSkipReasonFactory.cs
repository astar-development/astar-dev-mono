namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadSkipReason"/> values.</summary>
public static class DownloadSkipReasonFactory
{
    /// <summary>Creates the <see cref="DownloadSkipReason.RemoteGone"/> case.</summary>
    public static DownloadSkipReason CreateRemoteGone() => new DownloadSkipReason.RemoteGone();

    /// <summary>Creates the <see cref="DownloadSkipReason.LocalBusy"/> case.</summary>
    public static DownloadSkipReason CreateLocalBusy() => new DownloadSkipReason.LocalBusy();
}
