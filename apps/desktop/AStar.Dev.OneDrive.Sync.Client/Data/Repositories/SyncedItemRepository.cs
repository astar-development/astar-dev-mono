using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

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
        existing.FileDetailId = item.FileDetailId;
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return existing.Id;
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

        var tagCategoryIds = criteria.Tags.Count > 0 ? await ResolveTagCategoryIdsAsync(db, criteria.Tags, cancellationToken).ConfigureAwait(false) : null;
        string? localRoot = await GetLocalSyncRootAsync(db, criteria.AccountId, cancellationToken).ConfigureAwait(false);

        var combined = await QuerySyncedAsync(db, criteria, tagCategoryIds, cancellationToken).ConfigureAwait(false);

        if (localRoot is not null)
            combined.AddRange(await QueryUnsyncedAsync(db, criteria, localRoot, tagCategoryIds, cancellationToken).ConfigureAwait(false));

        if (criteria.DuplicatesOnly)
        {
            var duplicateKeys = await ResolveDuplicateKeysAsync(db, criteria.AccountId, localRoot, cancellationToken).ConfigureAwait(false);
            combined = [.. combined.Where(result => result.SizeInBytes != null && duplicateKeys.Contains((result.SizeInBytes.Value, DuplicateFileName(result))))];
        }

        return SortResults(combined, criteria.SortOrder);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDistinctTagNamesAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        string? localRoot = await GetLocalSyncRootAsync(db, accountId, cancellationToken).ConfigureAwait(false);
        var classifications = localRoot is null
            ? db.FileClassifications.Where(jt => db.SyncedItems.Any(i => (i.AccountId == accountId) && i.FileDetailId != null && i.FileDetailId == jt.FileDetailId))
            : db.FileClassifications.Where(jt => db.SyncedItems.Any(i => (i.AccountId == accountId) && i.FileDetailId != null && i.FileDetailId == jt.FileDetailId) || jt.FileDetail!.DirectoryName.Value.StartsWith(localRoot));

        var categories = await classifications
            .Select(jt => jt.Category!.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return categories;
    }

    /// <inheritdoc />
    public async Task DeleteFileDetailAsync(FileId fileDetailId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.FileClassifications.Where(jt => jt.FileDetailId == fileDetailId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        _ = await db.SyncedItems.Where(i => i.FileDetailId == fileDetailId).ExecuteUpdateAsync(setters => setters.SetProperty(i => i.FileDetailId, (FileId?)null), cancellationToken).ConfigureAwait(false);
        _ = await db.Files.Where(f => f.Id == fileDetailId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<SyncedItemSearchResult>> QuerySyncedAsync(AppDbContext db, SyncedItemSearchCriteria criteria, List<int>? tagCategoryIds, CancellationToken cancellationToken)
    {
        var query = db.SyncedItems.Where(i => i.AccountId == criteria.AccountId && !i.IsFolder);

        if (!string.IsNullOrEmpty(criteria.NameFragment))
            query = query.Where(i => i.RemotePath.Contains(criteria.NameFragment));

        if (criteria.MinBytes.HasValue)
            query = query.Where(i => i.SizeInBytes != null && i.SizeInBytes >= criteria.MinBytes.Value);

        if (criteria.MaxBytes.HasValue)
            query = query.Where(i => i.SizeInBytes != null && i.SizeInBytes <= criteria.MaxBytes.Value);

        if (tagCategoryIds is not null)
            query = query.Where(i => i.FileDetailId != null && db.FileClassifications.Any(jt => jt.FileDetailId == i.FileDetailId && tagCategoryIds.Contains(jt.CategoryId)));

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
                syncedItem.FileDetailId,
                TagNames = db.FileClassifications
                    .Where(jt => syncedItem.FileDetailId != null && jt.FileDetailId == syncedItem.FileDetailId)
                    .Select(jt => jt.Category!.Name)
                    .ToList()
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. items.Select(i => SyncedItemSearchResultFactory.Create(i.Id, i.AccountId, i.RemoteItemId, i.RemotePath, i.LocalPath, i.RemoteModifiedAt, i.SizeInBytes, i.TagNames, isSynced: true, i.FileDetailId))];
    }

    private static async Task<List<SyncedItemSearchResult>> QueryUnsyncedAsync(AppDbContext db, SyncedItemSearchCriteria criteria, string localRoot, List<int>? tagCategoryIds, CancellationToken cancellationToken)
    {
        var query = UnsyncedFilesUnderRoot(db, criteria.AccountId, localRoot);

        if (!string.IsNullOrEmpty(criteria.NameFragment))
            query = query.Where(f => (f.DirectoryName.Value + "/" + f.FileName.Value).Contains(criteria.NameFragment));

        if (criteria.MinBytes.HasValue)
            query = query.Where(f => f.FileSize >= criteria.MinBytes.Value);

        if (criteria.MaxBytes.HasValue)
            query = query.Where(f => f.FileSize <= criteria.MaxBytes.Value);

        if (tagCategoryIds is not null)
            query = query.Where(f => db.FileClassifications.Any(jt => jt.FileDetailId == f.Id && tagCategoryIds.Contains(jt.CategoryId)));

        var files = await query
            .Select(file => new
            {
                file.Id,
                FileName = file.FileName.Value,
                DirectoryName = file.DirectoryName.Value,
                file.FileSize,
                TagNames = db.FileClassifications
                    .Where(jt => jt.FileDetailId == file.Id)
                    .Select(jt => jt.Category!.Name)
                    .ToList()
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. files.Select(f => SyncedItemSearchResultFactory.Create(0, criteria.AccountId, new OneDriveItemId(string.Empty), string.Empty, $"{f.DirectoryName}/{f.FileName}", default, f.FileSize, f.TagNames, isSynced: false, f.Id))];
    }

    private static IQueryable<FileDetailEntity> UnsyncedFilesUnderRoot(AppDbContext db, AccountId accountId, string localRoot) => db.Files.Where(f => f.DirectoryName.Value.StartsWith(localRoot) && !db.SyncedItems.Any(s => s.AccountId == accountId && s.FileDetailId == f.Id));

    private static async Task<List<int>> ResolveTagCategoryIdsAsync(AppDbContext db, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        var tagList = tags.ToList();

        return await db.FileClassificationCategories
            .Where(c => tagList.Contains(c.Name))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string?> GetLocalSyncRootAsync(AppDbContext db, AccountId accountId, CancellationToken cancellationToken)
    {
        var accounts = await db.Accounts
            .Where(a => a.Id == accountId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        string? root = accounts.Count == 0 ? null : accounts[0].SyncConfig.LocalSyncPath.Value;

        return string.IsNullOrWhiteSpace(root) ? null : root;
    }

    private static async Task<HashSet<(long SizeInBytes, string FileName)>> ResolveDuplicateKeysAsync(AppDbContext db, AccountId accountId, string? localRoot, CancellationToken cancellationToken)
    {
        var syncedCandidates = await db.SyncedItems
            .Where(i => i.AccountId == accountId && !i.IsFolder && i.SizeInBytes != null)
            .Select(i => new { Size = i.SizeInBytes!.Value, i.RemotePath })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var keys = syncedCandidates.Select(i => (i.Size, FileName: i.RemotePath[(i.RemotePath.LastIndexOf('/') + 1)..]));

        if (localRoot is not null)
        {
            var unsyncedCandidates = await UnsyncedFilesUnderRoot(db, accountId, localRoot)
                .Select(f => new { Size = f.FileSize, FileName = f.FileName.Value })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            keys = keys.Concat(unsyncedCandidates.Select(f => (f.Size, f.FileName)));
        }

        return [.. keys.GroupBy(key => key).Where(g => g.Count() > 1).Select(g => g.Key)];
    }

    private static string DuplicateFileName(SyncedItemSearchResult result) => result.IsSynced ? result.RemotePath[(result.RemotePath.LastIndexOf('/') + 1)..] : Path.GetFileName(result.LocalPath);

    private static IReadOnlyList<SyncedItemSearchResult> SortResults(List<SyncedItemSearchResult> results, SearchSortOrder sortOrder) => sortOrder switch
    {
        SearchSortOrder.NameDescending => [.. results.OrderByDescending(SortPath, StringComparer.Ordinal)],
        SearchSortOrder.SizeAscending => [.. results.OrderBy(result => result.SizeInBytes)],
        SearchSortOrder.SizeDescending => [.. results.OrderByDescending(result => result.SizeInBytes)],
        _ => [.. results.OrderBy(SortPath, StringComparer.Ordinal)]
    };

    private static string SortPath(SyncedItemSearchResult result) => result.IsSynced ? result.RemotePath : result.LocalPath;
}
