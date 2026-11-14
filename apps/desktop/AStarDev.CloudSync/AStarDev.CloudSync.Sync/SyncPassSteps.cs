namespace AStarDev.CloudSync.Sync;

/// <summary>The four steps of a sync run, in the order they run.</summary>
/// <param name="remoteIndex">Step 1: brings the remote index up to date.</param>
/// <param name="localChanges">Step 2: scans the local side.</param>
/// <param name="download">Step 3: remote to local.</param>
/// <param name="upload">Step 4: local to remote.</param>
/// <param name="attention">Closes settled conflicts before the passes and records what they found.</param>
/// <param name="quota">Refreshes the account's storage usage after the passes.</param>
internal sealed class SyncPassSteps(IRemoteIndexUpdater remoteIndex, ILocalChangeDetector localChanges, IDownloadPass download, IUploadPass upload, SyncPassAttention attention, IAccountQuotaRefresher quota)
{
    /// <summary>Step 1: brings the remote index up to date.</summary>
    public IRemoteIndexUpdater RemoteIndex { get; } = remoteIndex;

    /// <summary>Step 2: scans the local side.</summary>
    public ILocalChangeDetector LocalChanges { get; } = localChanges;

    /// <summary>Step 3: remote to local.</summary>
    public IDownloadPass Download { get; } = download;

    /// <summary>Step 4: local to remote.</summary>
    public IUploadPass Upload { get; } = upload;

    /// <summary>The attention queue: conflicts and held deletions.</summary>
    public SyncPassAttention Attention { get; } = attention;

    /// <summary>Refreshes the account's storage usage after the passes.</summary>
    public IAccountQuotaRefresher Quota { get; } = quota;
}
