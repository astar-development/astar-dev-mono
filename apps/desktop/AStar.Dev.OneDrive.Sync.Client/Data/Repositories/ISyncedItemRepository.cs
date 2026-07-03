using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface ISyncedItemRepository
{
    /// <summary>Returns all synced items for the specified account, loaded into a dictionary keyed by remote item ID for fast lookups.</summary>
    Task<Dictionary<string, SyncedItemEntity>> GetAllByAccountAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Inserts or updates the synced item record. Returns the database Id of the entity after the operation.</summary>
    Task<int> UpsertAsync(SyncedItemEntity item, CancellationToken cancellationToken);

    /// <summary>Removes the synced item record with the specified remote item ID.</summary>
    Task DeleteByRemoteIdAsync(AccountId accountId, OneDriveItemId remoteItemId, CancellationToken cancellationToken);

    /// <summary>Removes all synced item records for the specified account whose remote item ID is in <paramref name="remoteIds"/>. Deletes are issued in chunks of at most 200 IDs to respect SQLite parameter limits.</summary>
    Task DeleteManyByRemoteIdAsync(AccountId accountId, IReadOnlyList<OneDriveItemId> remoteIds, CancellationToken cancellationToken);

    /// <summary>Removes all synced items for the specified account. Used when clearing state before a full re-enumeration.</summary>
    Task DeleteAllAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Searches synced items for the specified account using the provided criteria.</summary>
    Task<IReadOnlyList<SyncedItemSearchResult>> SearchAsync(SyncedItemSearchCriteria criteria, CancellationToken cancellationToken);

    /// <summary>Returns all distinct tag names for synced items belonging to the specified account.</summary>
    Task<IReadOnlyList<string>> GetDistinctTagNamesAsync(AccountId accountId, CancellationToken cancellationToken);
}
