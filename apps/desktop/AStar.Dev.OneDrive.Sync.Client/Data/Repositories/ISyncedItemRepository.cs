using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface ISyncedItemRepository
{
    /// <summary>Returns all synced items for the specified account, loaded into a dictionary keyed by remote item ID for fast lookups.</summary>
    Task<Dictionary<string, SyncedItemEntity>> GetAllByAccountAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Inserts or updates the synced item record.</summary>
    Task UpsertAsync(SyncedItemEntity item, CancellationToken cancellationToken);

    /// <summary>Removes the synced item record with the specified remote item ID.</summary>
    Task DeleteByRemoteIdAsync(AccountId accountId, OneDriveItemId remoteItemId, CancellationToken cancellationToken);

    /// <summary>Removes all synced items for the specified account. Used when clearing state before a full re-enumeration.</summary>
    Task DeleteAllAsync(AccountId accountId, CancellationToken cancellationToken);
}
