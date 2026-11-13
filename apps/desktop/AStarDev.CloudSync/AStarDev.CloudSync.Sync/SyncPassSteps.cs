namespace AStarDev.CloudSync.Sync;

/// <summary>The four steps of a sync run, in the order they run.</summary>
/// <param name="remoteIndex">Step 1: brings the remote index up to date.</param>
/// <param name="localChanges">Step 2: scans the local side.</param>
/// <param name="download">Step 3: remote to local.</param>
/// <param name="upload">Step 4: local to remote.</param>
internal sealed class SyncPassSteps(IRemoteIndexUpdater remoteIndex, ILocalChangeDetector localChanges, IDownloadPass download, IUploadPass upload)
{
    /// <summary>Step 1: brings the remote index up to date.</summary>
    public IRemoteIndexUpdater RemoteIndex { get; } = remoteIndex;

    /// <summary>Step 2: scans the local side.</summary>
    public ILocalChangeDetector LocalChanges { get; } = localChanges;

    /// <summary>Step 3: remote to local.</summary>
    public IDownloadPass Download { get; } = download;

    /// <summary>Step 4: local to remote.</summary>
    public IUploadPass Upload { get; } = upload;
}
