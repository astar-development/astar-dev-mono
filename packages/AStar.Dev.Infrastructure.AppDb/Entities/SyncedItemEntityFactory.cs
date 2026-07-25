using System.IO.Abstractions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Factory class for creating instances of <see cref="SyncedItemEntity"/> from various sources such as remote delta items and synchronization jobs.
/// </summary>
public static class SyncedItemEntityFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="SyncedItemEntity"/> based on the provided account ID, delta item from the OneDrive API, and the corresponding remote and local paths.
    /// </summary>
    /// <param name="accountId">The ID of the account to which the item belongs.</param>
    /// <param name="item">The delta item from the OneDrive API.</param>
    /// <param name="remotePath">The remote path of the item in OneDrive.</param>
    /// <param name="localPath">The local path of the item on the user's file system.</param>
    /// <returns>A new instance of <see cref="SyncedItemEntity"/>.</returns>
    public static SyncedItemEntity Create(AccountId accountId, FileDeltaItem item, string remotePath, string localPath)
        => new()
        {
            AccountId = accountId,
            RemoteItemId = item.Id,
            RemoteParentId = item.ParentId.Match(id => id.Id, () => string.Empty),
            RemotePath = remotePath,
            LocalPath = localPath,
            IsFolder = false,
            RemoteModifiedAt = item.LastModified.MapOrDefault(v => v, DateTimeOffset.MinValue),
            Tags = item.VersionInfo,
            SizeInBytes = item.Size
        };

    /// <summary>
    /// Creates a new instance of <see cref="SyncedItemEntity"/> based on the provided account ID, delta item representing a folder from the OneDrive API, and the corresponding remote and local paths.
    /// </summary>
    /// <param name="accountId">The ID of the account to which the item belongs.</param>
    /// <param name="item">The delta item representing a folder from the OneDrive API.</param>
    /// <param name="remotePath">The remote path of the folder in OneDrive.</param>
    /// <param name="localPath">The local path of the folder on the user's file system.</param>
    /// <returns>A new instance of <see cref="SyncedItemEntity"/>.</returns>
    public static SyncedItemEntity Create(AccountId accountId, FolderDeltaItem item, string remotePath, string localPath)
        => new()
        {
            AccountId = accountId,
            RemoteItemId = item.Id,
            RemoteParentId = item.ParentId.Match(id => id.Id, () => string.Empty),
            RemotePath = remotePath,
            LocalPath = localPath,
            IsFolder = true,
            RemoteModifiedAt = DateTimeOffset.MinValue,
            Tags = item.VersionInfo,
            SizeInBytes = null
        };

    /// <summary>
    /// Creates a tracking entity for a successfully completed download job.
    /// </summary>
    /// <param name="accountId">The ID of the account to which the item belongs.</param>
    /// <param name="job">The download job that was completed.</param>
    /// <param name="remotePath">The remote path of the item in OneDrive.</param>
    /// <returns>A new instance of <see cref="SyncedItemEntity"/>.</returns>
    public static SyncedItemEntity CreateFromDownloadJob(AccountId accountId, SyncJob job, string remotePath)
        => new()
        {
            AccountId = accountId,
            RemoteItemId = job.Remote.RemoteItemId,
            RemoteParentId = string.Empty,
            RemotePath = remotePath,
            LocalPath = job.Target.LocalPath,
            IsFolder = false,
            RemoteModifiedAt = job.Metadata.RemoteModified,
            Tags = job.Metadata.VersionInfo.Match(v => v, () => VersionInfoFactory.Create(Option.None<string>(), Option.None<string>())),
            SizeInBytes = job.Metadata.FileSize
        };

    /// <summary>
    /// Creates a tracking entity for a successfully completed upload job.
    /// <paramref name="uploadedRemoteItemId"/> must be the unwrapped remote item ID assigned by OneDrive after upload;
    /// callers are responsible for verifying the value is present before invoking this method.
    /// </summary>
    /// <param name="accountId">The ID of the account to which the item belongs.</param>
    /// <param name="job">The upload job that was completed.</param>
    /// <param name="uploadedRemoteItemId">The remote item ID returned by OneDrive for the uploaded file.</param>
    /// <param name="remotePath">The remote path of the item in OneDrive.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <returns>A new instance of <see cref="SyncedItemEntity"/>.</returns>
    public static SyncedItemEntity CreateFromUploadJob(AccountId accountId, UploadSyncJob job, string uploadedRemoteItemId, string remotePath, IFileSystem fileSystem)
        => new()
        {
            AccountId = accountId,
            RemoteItemId = new OneDriveItemId(uploadedRemoteItemId),
            RemoteParentId = string.Empty,
            RemotePath = remotePath,
            LocalPath = job.Target.LocalPath,
            IsFolder = false,
            RemoteModifiedAt = new DateTimeOffset(fileSystem.FileInfo.New(job.Target.LocalPath).LastWriteTimeUtc, TimeSpan.Zero),
            SizeInBytes = fileSystem.FileInfo.New(job.Target.LocalPath).Length
        };
}
