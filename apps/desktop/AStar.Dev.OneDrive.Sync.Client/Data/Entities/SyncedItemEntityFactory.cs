using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// Factory class for creating instances of <see cref="SyncedItemEntity"/> from various sources such as remote delta items and synchronization jobs. This class centralizes the logic for constructing SyncedItemEntity objects, ensuring consistency and maintainability in how synchronized items are represented within the sync client application. By providing specific factory methods for different scenarios, we can simplify the creation process and reduce duplication of code across the application when dealing with synchronized items.
/// </summary>
public static class SyncedItemEntityFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="SyncedItemEntity"/> based on the provided account ID, delta item from the OneDrive API, and the corresponding remote and local paths. This method extracts relevant information from the delta item, such as the item ID, parent ID, modification timestamp, and version info, to populate the properties of the SyncedItemEntity. The resulting entity can then be used to track the synchronization state of the item within the sync client application, allowing for efficient updates and conflict resolution as needed.
    /// </summary>
    /// <param name="accountId">The ID of the account to which the item belongs.</param>
    /// <param name="item">The delta item from the OneDrive API.</param>
    /// <param name="remotePath">The remote path of the item in OneDrive.</param>
    /// <param name="localPath">The local path of the item on the user's file system.</param>
    /// <returns>A new instance of <see cref="SyncedItemEntity"/>.</returns>
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
            Tags             = item.VersionInfo
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
            AccountId        = accountId,
            RemoteItemId     = job.Remote.RemoteItemId,
            RemoteParentId   = string.Empty,
            RemotePath       = remotePath,
            LocalPath        = job.Target.LocalPath,
            IsFolder         = false,
            RemoteModifiedAt = job.Metadata.RemoteModified,
            Tags             = job.Metadata.VersionInfo ?? new VersionInfo(null, null)
        };

    /// <summary>
    /// Creates a tracking entity for a successfully completed upload job.
    /// </summary>
    /// <param name="accountId">The ID of the account to which the item belongs.</param>
    /// <param name="job">The upload job that was completed.</param>
    /// <param name="remotePath">The remote path of the item in OneDrive.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <returns>A new instance of <see cref="SyncedItemEntity"/>.</returns>
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
