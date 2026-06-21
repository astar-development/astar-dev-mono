using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class SyncedItemRepository(IDbContextFactory<AppDbContext> dbFactory) : ISyncedItemRepository
{
    private const int DeleteChunkSize = 200;

    public async Task<Dictionary<string, SyncedItemEntity>> GetAllByAccountAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var items = await db.SyncedItems
            .Where(i => i.AccountId == accountId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return items.ToDictionary(i => i.RemoteItemId.Id);
    }

    public async Task<int> UpsertAsync(SyncedItemEntity item, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.SyncedItems
            .FirstOrDefaultAsync(i => i.AccountId == item.AccountId && i.RemoteItemId == item.RemoteItemId, cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            _ = db.SyncedItems.Add(item);
            _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return item.Id;
        }

        existing.RemoteParentId = item.RemoteParentId;
        existing.RemotePath = item.RemotePath;
        existing.LocalPath = item.LocalPath;
        existing.IsFolder = item.IsFolder;
        existing.RemoteModifiedAt = item.RemoteModifiedAt;
        existing.Tags = item.Tags;
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return existing.Id;
    }

    public async Task UpsertFileClassificationsAsync(int syncedItemId, IReadOnlyList<int> categoryIds, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.SyncedItemFileClassifications
                   .Where(c => c.SyncedItemId == syncedItemId)
                   .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        var entities = categoryIds.Select(categoryId => new SyncedItemFileClassificationEntity
        {
            SyncedItemId = syncedItemId,
            CategoryId = categoryId
        });

        db.SyncedItemFileClassifications.AddRange(entities);
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> UpsertWithClassificationsAsync(SyncedItemEntity item, IReadOnlyList<int> categoryIds, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        int syncedItemId = await UpsertInContextAsync(db, item, cancellationToken).ConfigureAwait(false);

        _ = await db.SyncedItemFileClassifications
                   .Where(c => c.SyncedItemId == syncedItemId)
                   .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        var classificationEntities = categoryIds.Select(categoryId => new SyncedItemFileClassificationEntity
        {
            SyncedItemId = syncedItemId,
            CategoryId = categoryId
        });

        db.SyncedItemFileClassifications.AddRange(classificationEntities);
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return syncedItemId;
    }

    public async Task DeleteByRemoteIdAsync(AccountId accountId, OneDriveItemId remoteItemId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.SyncedItems
                   .Where(i => i.AccountId == accountId && i.RemoteItemId == remoteItemId)
                   .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteManyByRemoteIdAsync(AccountId accountId, IReadOnlyList<OneDriveItemId> remoteIds, CancellationToken cancellationToken)
    {
        if (remoteIds.Count == 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        for (int offset = 0; offset < remoteIds.Count; offset += DeleteChunkSize)
        {
            var chunk = remoteIds.Skip(offset).Take(DeleteChunkSize).ToList();

            _ = await db.SyncedItems
                       .Where(item => item.AccountId == accountId && chunk.Contains(item.RemoteItemId))
                       .ExecuteDeleteAsync(cancellationToken)
                       .ConfigureAwait(false);
        }
    }

    public async Task DeleteAllAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.SyncedItems
                   .Where(i => i.AccountId == accountId)
                   .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SyncedItemSearchResult>> SearchAsync(SyncedItemSearchCriteria criteria, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var query = db.SyncedItems.Where(i => i.AccountId == criteria.AccountId && !i.IsFolder);

        if (!string.IsNullOrEmpty(criteria.NameFragment))
            query = query.Where(i => i.RemotePath.Contains(criteria.NameFragment));

        if (criteria.MinBytes.HasValue)
            query = query.Where(i => i.SizeInBytes != null && i.SizeInBytes >= criteria.MinBytes.Value);

        if (criteria.MaxBytes.HasValue)
            query = query.Where(i => i.SizeInBytes != null && i.SizeInBytes <= criteria.MaxBytes.Value);

        if (criteria.Tags.Count > 0)
        {
            var tagList = criteria.Tags.ToList();
            var tagCategoryIds = await db.FileClassificationCategories
                .Where(c => tagList.Contains(c.Name))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            query = query.Where(i => db.SyncedItemFileClassifications
                .Any(jt => jt.SyncedItemId == i.Id && tagCategoryIds.Contains(jt.CategoryId)));
        }

        if (criteria.DuplicatesOnly)
        {
            var duplicateIds = await ResolveDuplicateIdsAsync(db, criteria.AccountId, cancellationToken).ConfigureAwait(false);

            if (duplicateIds.Count == 0)
                return [];

            query = query.Where(i => duplicateIds.Contains(i.Id));
        }

        query = criteria.SortOrder switch
        {
            SearchSortOrder.NameDescending => query.OrderByDescending(i => i.RemotePath),
            SearchSortOrder.SizeAscending => query.OrderBy(i => i.SizeInBytes),
            SearchSortOrder.SizeDescending => query.OrderByDescending(i => i.SizeInBytes),
            _ => query.OrderBy(i => i.RemotePath)
        };

        var items = await query
            .Select(syncedItem => new
            {
                syncedItem.Id,
                syncedItem.AccountId,
                syncedItem.RemoteItemId,
                syncedItem.RemotePath,
                syncedItem.LocalPath,
                syncedItem.RemoteModifiedAt,
                syncedItem.SizeInBytes,
                TagNames = db.SyncedItemFileClassifications
                    .Where(jt => jt.SyncedItemId == syncedItem.Id)
                    .Select(jt => jt.Category!.Name)
                    .ToList()
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return items.Select(i => SyncedItemSearchResultFactory.Create(i.Id, i.AccountId, i.RemoteItemId, i.RemotePath, i.LocalPath, i.RemoteModifiedAt, i.SizeInBytes, i.TagNames)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctTagNamesAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var categories = await db.SyncedItemFileClassifications
            .Where(jt => jt.SyncedItem!.AccountId == accountId)
            .Select(jt => jt.Category!.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return categories;
    }

    private static async Task<HashSet<int>> ResolveDuplicateIdsAsync(AppDbContext db, AccountId accountId, CancellationToken cancellationToken)
    {
        var sizesWithDuplicates = await db.SyncedItems
            .Where(i => i.AccountId == accountId && !i.IsFolder && i.SizeInBytes != null)
            .GroupBy(i => i.SizeInBytes)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (sizesWithDuplicates.Count == 0)
            return [];

        var candidates = await db.SyncedItems
            .Where(i => i.AccountId == accountId && !i.IsFolder && sizesWithDuplicates.Contains(i.SizeInBytes))
            .Select(i => new { i.Id, i.SizeInBytes, i.RemotePath })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. candidates
            .GroupBy(i => new { i.SizeInBytes, FileName = i.RemotePath[(i.RemotePath.LastIndexOf('/') + 1)..] })
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(i => i.Id))];
    }

    private static async Task<int> UpsertInContextAsync(AppDbContext db, SyncedItemEntity item, CancellationToken cancellationToken)
    {
        var existing = await db.SyncedItems
            .FirstOrDefaultAsync(i => i.AccountId == item.AccountId && i.RemoteItemId == item.RemoteItemId, cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            _ = db.SyncedItems.Add(item);
            _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return item.Id;
        }

        existing.RemoteParentId = item.RemoteParentId;
        existing.RemotePath = item.RemotePath;
        existing.LocalPath = item.LocalPath;
        existing.IsFolder = item.IsFolder;
        existing.RemoteModifiedAt = item.RemoteModifiedAt;
        existing.Tags = item.Tags;
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return existing.Id;
    }
}
