using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;

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
            RemoteItemId     = new OneDriveItemId(item.Id),
            RemoteParentId   = item.ParentId ?? string.Empty,
            RemotePath       = remotePath,
            LocalPath        = localPath,
            IsFolder         = item.IsFolder,
            RemoteModifiedAt = item.LastModified ?? DateTimeOffset.MinValue,
            ETag             = item.ETag,
            CTag             = item.CTag
        };

    /// <summary>Creates a tracking entity for a successfully completed download job.</summary>
    public static SyncedItemEntity CreateFromDownloadJob(AccountId accountId, SyncJob job, string remotePath)
        => new()
        {
            AccountId        = accountId,
            RemoteItemId     = new OneDriveItemId(job.RemoteItemId),
            RemoteParentId   = string.Empty,
            RemotePath       = remotePath,
            LocalPath        = job.LocalPath,
            IsFolder         = false,
            RemoteModifiedAt = job.RemoteModified
        };

    /// <summary>Creates a tracking entity for a successfully completed upload job.</summary>
    public static SyncedItemEntity CreateFromUploadJob(AccountId accountId, SyncJob job, string remotePath, IFileSystem fileSystem)
        => new()
        {
            AccountId        = accountId,
            RemoteItemId     = new OneDriveItemId(job.UploadedRemoteItemId!),
            RemoteParentId   = string.Empty,
            RemotePath       = remotePath,
            LocalPath        = job.LocalPath,
            IsFolder         = false,
            RemoteModifiedAt = new DateTimeOffset(fileSystem.FileInfo.New(job.LocalPath).LastWriteTimeUtc, TimeSpan.Zero)
        };
}
