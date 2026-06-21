using System.Collections.Concurrent;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <summary>
/// Registers synced items in the database for folders and phantom files encountered during enumeration.
/// </summary>
public interface ISyncedItemRegistrar
{
    /// <summary>Creates the local directory and upserts the folder tracking entity.</summary>
    Task RegisterFolderAsync(AccountId accountId, FolderDeltaItem item, string remotePath, string localPath, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct);

    /// <summary>
    /// Registers a phantom file — exists locally with no tracking record — as already synced.
    /// The caller must supply the preloaded <paramref name="mappings"/> for classification; this method
    /// does not reload them to avoid one DB round-trip per phantom file.
    /// </summary>
    Task RegisterPhantomAsync(AccountId accountId, FileDeltaItem item, string remotePath, string localPath, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, IReadOnlyList<FileClassificationCategory> mappings, CancellationToken ct);

    /// <summary>
    /// Creates the tracking entity for a successfully completed download job, classifies it using
    /// <paramref name="mappings"/> and the auto-categoriser, then persists both atomically.
    /// The caller must supply the preloaded <paramref name="mappings"/> to avoid a redundant DB round-trip per job.
    /// </summary>
    Task RegisterDownloadAsync(AccountId accountId, SyncJob job, string remotePath, IReadOnlyList<FileClassificationCategory> mappings, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct);

    /// <summary>
    /// Creates the tracking entity for a successfully completed upload job using <paramref name="uploadedRemoteItemId"/>
    /// returned by OneDrive, classifies it using <paramref name="mappings"/> and the auto-categoriser, then persists
    /// both atomically.
    /// The caller must supply the preloaded <paramref name="mappings"/> to avoid a redundant DB round-trip per job.
    /// </summary>
    Task RegisterUploadAsync(AccountId accountId, UploadSyncJob job, string uploadedRemoteItemId, string remotePath, IReadOnlyList<FileClassificationCategory> mappings, ConcurrentDictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct);
}
