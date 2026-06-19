using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface ISyncedItemRepository
{
    /// <summary>Returns all synced items for the specified account, loaded into a dictionary keyed by remote item ID for fast lookups.</summary>
    Task<Dictionary<string, SyncedItemEntity>> GetAllByAccountAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Inserts or updates the synced item record. Returns the database Id of the entity after the operation.</summary>
    Task<int> UpsertAsync(SyncedItemEntity item, CancellationToken cancellationToken);

    /// <summary>Replaces all junction rows for the specified synced item with rows pointing to the provided category IDs.</summary>
    Task UpsertFileClassificationsAsync(int syncedItemId, IReadOnlyList<int> categoryIds, CancellationToken cancellationToken);

    /// <summary>Inserts or updates the synced item record and replaces its file classification junction rows atomically in a single <see cref="Microsoft.EntityFrameworkCore.DbContext"/> and transaction.</summary>
    Task<int> UpsertWithClassificationsAsync(SyncedItemEntity item, IReadOnlyList<int> categoryIds, CancellationToken cancellationToken);

    /// <summary>Removes the synced item record with the specified remote item ID.</summary>
    Task DeleteByRemoteIdAsync(AccountId accountId, OneDriveItemId remoteItemId, CancellationToken cancellationToken);

    /// <summary>Removes all synced items for the specified account. Used when clearing state before a full re-enumeration.</summary>
    Task DeleteAllAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Searches synced items for the specified account using the provided criteria.</summary>
    Task<IReadOnlyList<SyncedItemSearchResult>> SearchAsync(SyncedItemSearchCriteria criteria, CancellationToken cancellationToken);

    /// <summary>Returns all distinct tag names for synced items belonging to the specified account.</summary>
    Task<IReadOnlyList<string>> GetDistinctTagNamesAsync(AccountId accountId, CancellationToken cancellationToken);
}
