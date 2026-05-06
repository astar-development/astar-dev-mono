using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Domain;


namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// Creates <see cref="SyncedItemEntity"/> instances from remote delta items and completed sync jobs.
/// </summary>
public static class SyncedItemEntityFactory
{
    /// <summary>Creates a tracking entity from a remote delta item (folder, file, or phantom).</summary>
    public static SyncedItemEntity Create(AccountId accountId, DeltaItem item, string remotePath, string localPath)
        => new()
        {
            AccountId        = accountId,
            RemoteItemId     = item.Id,
            RemoteParentId   = item.ParentId?.Id ?? string.Empty,
            RemotePath       = remotePath,
            LocalPath        = localPath,
            IsFolder         = item.IsFolder,
            RemoteModifiedAt = item.LastModified ?? DateTimeOffset.MinValue,
            ETag             = item.VersionInfo.ETag,
            CTag             = item.VersionInfo.CTag
        };

    /// <summary>Creates a tracking entity for a successfully completed download job.</summary>
    public static SyncedItemEntity CreateFromDownloadJob(AccountId accountId, SyncJob job, string remotePath)
        => new()
        {
            AccountId        = accountId,
            RemoteItemId     = job.Remote.RemoteItemId,
            RemoteParentId   = string.Empty,
            RemotePath       = remotePath,
            LocalPath        = job.Target.LocalPath,
            IsFolder         = false,
            RemoteModifiedAt = job.Metadata.RemoteModified
        };

    /// <summary>Creates a tracking entity for a successfully completed upload job.</summary>
    public static SyncedItemEntity CreateFromUploadJob(AccountId accountId, UploadSyncJob job, string remotePath, IFileSystem fileSystem)
        => new()
        {
            AccountId        = accountId,
            RemoteItemId     = new OneDriveItemId(job.UploadedRemoteItemId!),
            RemoteParentId   = string.Empty,
            RemotePath       = remotePath,
            LocalPath        = job.Target.LocalPath,
            IsFolder         = false,
            RemoteModifiedAt = new DateTimeOffset(fileSystem.FileInfo.New(job.Target.LocalPath).LastWriteTimeUtc, TimeSpan.Zero)
        };
}
