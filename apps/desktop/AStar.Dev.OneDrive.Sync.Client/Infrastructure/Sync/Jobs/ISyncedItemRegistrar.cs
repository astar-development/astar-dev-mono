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
    Task RegisterFolderAsync(AccountId accountId, FolderDeltaItem item, string remotePath, string localPath, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct);

    /// <summary>
    /// Registers a phantom file — exists locally with no tracking record — as already synced.
    /// The caller must supply the preloaded <paramref name="mappings"/> for classification; this method
    /// does not reload them to avoid one DB round-trip per phantom file.
    /// </summary>
    Task RegisterPhantomAsync(AccountId accountId, FileDeltaItem item, string remotePath, string localPath, Dictionary<string, SyncedItemEntity> syncedItems, IReadOnlyList<FileClassificationCategory> mappings, CancellationToken ct);
}
