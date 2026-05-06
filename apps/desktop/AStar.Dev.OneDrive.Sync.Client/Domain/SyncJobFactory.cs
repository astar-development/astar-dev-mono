namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Creates typed <see cref="SyncJob"/> instances with auto-generated identity fields.</summary>
public static class SyncJobFactory
{
    /// <inheritdoc cref="DownloadSyncJob"/>
    public static DownloadSyncJob CreateDownload(RemoteItemRef remote, SyncFileTarget target, SyncFileMetadata metadata, string? downloadUrl = null)
        => new(remote, target, metadata, SyncJobStatusFactory.Create(), downloadUrl);

    /// <inheritdoc cref="UploadSyncJob"/>
    public static UploadSyncJob CreateUpload(RemoteItemRef remote, SyncFileTarget target, SyncFileMetadata metadata)
        => new(remote, target, metadata, SyncJobStatusFactory.Create());

    /// <inheritdoc cref="DeleteSyncJob"/>
    public static DeleteSyncJob CreateDelete(RemoteItemRef remote, SyncFileTarget target, SyncFileMetadata metadata)
        => new(remote, target, metadata, SyncJobStatusFactory.Create());
}
