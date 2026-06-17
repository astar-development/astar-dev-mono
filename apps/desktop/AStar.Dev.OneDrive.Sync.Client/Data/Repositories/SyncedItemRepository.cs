using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class SyncedItemRepository(IDbContextFactory<AppDbContext> dbFactory) : ISyncedItemRepository
{
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

        if(existing is null)
        {
            _ = db.SyncedItems.Add(item);
            _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return item.Id;
        }

        existing.RemoteParentId   = item.RemoteParentId;
        existing.RemotePath       = item.RemotePath;
        existing.LocalPath        = item.LocalPath;
        existing.IsFolder         = item.IsFolder;
        existing.RemoteModifiedAt = item.RemoteModifiedAt;
        existing.Tags             = item.Tags;
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
            CategoryId   = categoryId
        });

        db.SyncedItemFileClassifications.AddRange(entities);
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteByRemoteIdAsync(AccountId accountId, OneDriveItemId remoteItemId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.SyncedItems
                   .Where(i => i.AccountId == accountId && i.RemoteItemId == remoteItemId)
                   .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
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
            var candidates = await db.SyncedItems
                .Where(i => i.AccountId == criteria.AccountId && !i.IsFolder && i.SizeInBytes != null)
                .Select(i => new { i.Id, i.SizeInBytes, FileName = i.RemotePath.Substring(i.RemotePath.LastIndexOf('/') + 1) })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var duplicateIds = candidates
                .GroupBy(i => new { i.SizeInBytes, i.FileName })
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.Select(i => i.Id))
                .ToHashSet();

            query = query.Where(i => duplicateIds.Contains(i.Id));
        }

        query = criteria.SortOrder switch
        {
            SearchSortOrder.NameDescending => query.OrderByDescending(i => i.RemotePath),
            SearchSortOrder.SizeAscending  => query.OrderBy(i => i.SizeInBytes),
            SearchSortOrder.SizeDescending => query.OrderByDescending(i => i.SizeInBytes),
            _                              => query.OrderBy(i => i.RemotePath)
        };

        var items = await query
            .Select(i => new
            {
                i.Id,
                i.AccountId,
                i.RemoteItemId,
                i.RemotePath,
                i.LocalPath,
                i.RemoteModifiedAt,
                i.SizeInBytes,
                TagNames = db.SyncedItemFileClassifications
                    .Where(jt => jt.SyncedItemId == i.Id)
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
}
