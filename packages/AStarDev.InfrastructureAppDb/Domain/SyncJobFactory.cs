using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Creates typed <see cref="SyncJob"/> instances with auto-generated identity fields.</summary>
public static class SyncJobFactory
{
    /// <inheritdoc cref="DownloadSyncJob"/>
    public static DownloadSyncJob CreateDownload(RemoteItemRef remote, SyncFileTarget target, SyncFileMetadata metadata) => new(remote, target, metadata, SyncJobStatusFactory.Create(), Option.None<string>());

    /// <inheritdoc cref="DownloadSyncJob"/>
    public static DownloadSyncJob CreateDownload(RemoteItemRef remote, SyncFileTarget target, SyncFileMetadata metadata, Option<string> downloadUrl) => new(remote, target, metadata, SyncJobStatusFactory.Create(), downloadUrl);

    /// <inheritdoc cref="UploadSyncJob"/>
    public static UploadSyncJob CreateUpload(RemoteItemRef remote, SyncFileTarget target, SyncFileMetadata metadata) => new(remote, target, metadata, SyncJobStatusFactory.Create(), Option.None<string>());

    /// <inheritdoc cref="DeleteSyncJob"/>
    public static DeleteSyncJob CreateDelete(RemoteItemRef remote, SyncFileTarget target, SyncFileMetadata metadata) => new(remote, target, metadata, SyncJobStatusFactory.Create());
}
