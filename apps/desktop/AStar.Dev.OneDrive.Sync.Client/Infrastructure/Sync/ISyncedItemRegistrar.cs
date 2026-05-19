using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Registers synced items in the database for folders and phantom files encountered during enumeration.
/// </summary>
public interface ISyncedItemRegistrar
{
    /// <summary>Creates the local directory and upserts the folder tracking entity.</summary>
    Task RegisterFolderAsync(AccountId accountId, DeltaItem item, string remotePath, string localPath, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct);

    /// <summary>Registers a phantom file — exists locally with no tracking record — as already synced.</summary>
    Task RegisterPhantomAsync(AccountId accountId, DeltaItem item, string remotePath, string localPath, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct);
}
