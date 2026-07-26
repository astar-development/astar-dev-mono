using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

/// <inheritdoc />
public sealed class FileClassificationRepository(IDbContextFactory<AppDbContext> dbFactory, ILogger<FileClassificationRepository> logger) : IFileClassificationRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<FileClassificationCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await db.FileClassificationCategories.Where(c => c.IncludeInSearch && c.Name != "Unclassified").ToListAsync(cancellationToken).ConfigureAwait(false);

        var categories = new List<FileClassificationCategory>(entities.Count);
        foreach (var e in entities)
        {
            var result = FileClassificationCategoryFactory.Create(
                new FileClassificationCategoryId(e.Id),
                e.Name,
                e.Level,
                e.IsFamous,
                e.IsInternet,
                e.ParentId.HasValue ? Option.Some(new FileClassificationCategoryId(e.ParentId.Value)) : Option.None<FileClassificationCategoryId>(),
                e.IncludeInSearch
            );

            result.Tap(
                ok => categories.Add(ok),
                err => OneDriveSyncClientMessages.ClassificationRowSkipped(logger, e.Id, err));
        }

        return categories.AsReadOnly();
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<FileClassificationCategoryEntity>> GetAllCategoriesSimpleAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await db.FileClassificationCategories.ToListAsync(cancellationToken).ConfigureAwait(false);

        return entities.AsReadOnly();
    }

    /// <inheritdoc />
    public Task<Result<FileClassificationCategoryId, string>> AddCategoryAsync(FileClassificationCategory category, CancellationToken cancellationToken = default)
        => Try.RunAsync(async () =>
            {
                await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

                var entity = new FileClassificationCategoryEntity
                {
                    Name = category.Name.ToTitleCase(),
                    Level = category.Level,
                    IsFamous = category.IsFamous,
                    IsInternet = category.IsInternet,
                    ParentId = category.ParentId.MapOrDefault(pid => (int?)pid.Id, null),
                    IncludeInSearch = category.IncludeInSearch
                };

                db.FileClassificationCategories.Add(entity);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return new FileClassificationCategoryId(entity.Id);
            }).ToResultAsync(ex => ex.GetBaseException().Message);

    /// <inheritdoc />
    public async Task<Result<FileClassificationCategoryId, string>> UpdateCategoryAsync(FileClassificationCategoryId id, FileClassificationCategory category, CancellationToken cancellationToken = default)
    {
        int rawId = id.Id;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await db.FileClassificationCategories.FindAsync([rawId], cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new Fail<FileClassificationCategoryId, string>("Category not found.");

        entity.Name = category.Name.ToTitleCase();
        entity.Level = category.Level;
        entity.IsFamous = category.IsFamous;
        entity.IsInternet = category.IsInternet;
        entity.ParentId = category.ParentId.MapOrDefault(pid => (int?)pid.Id, null);
        entity.IncludeInSearch = category.IncludeInSearch;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Ok<FileClassificationCategoryId, string>(id);
    }

    /// <inheritdoc />
    public async Task<Result<FileClassificationCategoryId, string>> ReparentCategoryAsync(FileClassificationCategoryId id, Option<FileClassificationCategoryId> newParentId, CancellationToken cancellationToken = default)
    {
        int rawId = id.Id;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await db.FileClassificationCategories.FindAsync([rawId], cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new Fail<FileClassificationCategoryId, string>("Category not found.");

        var descendantIds = await GetDescendantIdsAsync(db, rawId, cancellationToken).ConfigureAwait(false);

        int newLevel = 1;
        if (newParentId is Option<FileClassificationCategoryId>.Some someParent)
        {
            int newParentRawId = someParent.Value.Id;
            if (newParentRawId == rawId || descendantIds.Contains(newParentRawId))
                return new Fail<FileClassificationCategoryId, string>("Cannot reparent a category under itself or one of its own descendants.");

            var parentEntity = await db.FileClassificationCategories.FindAsync([newParentRawId], cancellationToken).ConfigureAwait(false);
            if (parentEntity is null)
                return new Fail<FileClassificationCategoryId, string>("Parent category not found.");

            newLevel = parentEntity.Level + 1;
        }

        int levelDelta = newLevel - entity.Level;
        entity.ParentId = newParentId.MapOrDefault(parentId => (int?)parentId.Id, null);
        entity.Level = newLevel;

        if (levelDelta != 0 && descendantIds.Count > 0)
        {
            var descendants = await db.FileClassificationCategories.Where(c => descendantIds.Contains(c.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var descendant in descendants)
                descendant.Level += levelDelta;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Ok<FileClassificationCategoryId, string>(id);
    }

    private static async Task<HashSet<int>> GetDescendantIdsAsync(AppDbContext db, int rootId, CancellationToken cancellationToken)
    {
        var allParentLinks = await db.FileClassificationCategories.Select(c => new { c.Id, c.ParentId }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var childrenByParentId = allParentLinks.Where(c => c.ParentId.HasValue).ToLookup(c => c.ParentId!.Value);

        var descendantIds = new HashSet<int>();
        var pendingIds = new Queue<int>();
        pendingIds.Enqueue(rootId);

        while (pendingIds.Count > 0)
        {
            int currentId = pendingIds.Dequeue();
            foreach (var child in childrenByParentId[currentId])
            {
                if (descendantIds.Add(child.Id))
                    pendingIds.Enqueue(child.Id);
            }
        }

        return descendantIds;
    }

    /// <inheritdoc />
    public async Task DeleteCategoryAsync(FileClassificationCategoryId id, CancellationToken cancellationToken = default)
    {
        int rawId = id.Id;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var subCategory = await db.FileClassificationCategories.FirstOrDefaultAsync(c => c.ParentId == rawId, cancellationToken).ConfigureAwait(false);
        if (subCategory is not null)
            db.FileClassificationCategories.Remove(subCategory);

        var entity = await db.FileClassificationCategories.FindAsync([rawId], cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return;

        db.FileClassificationCategories.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> HasClassificationsAsync(FileId fileDetailId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.FileClassifications.AnyAsync(c => c.FileDetailId == fileDetailId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddClassificationsAsync(FileId fileDetailId, IReadOnlyList<int> categoryIds, CancellationToken cancellationToken = default)
    {
        if (categoryIds.Count == 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        db.FileClassifications.AddRange(categoryIds.Select(categoryId => new FileClassificationEntity { FileDetailId = fileDetailId, CategoryId = categoryId }));
        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IEnumerable<FileClassificationCategoryId> fileClassificationCategoryIds, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        int[] categoryIds = [.. fileClassificationCategoryIds.Select(id => id.Id)];
        db.FileClassificationCategories.RemoveRange(db.FileClassificationCategories.Where(c => categoryIds.Contains(c.Id)));
        //_ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); // need to rethink this
    }

    private static KeywordMapping BuildKeywordMapping(FileClassificationKeywordEntity keyword, Dictionary<int, FileClassificationCategoryEntity> categoryById)
    {
        var ancestorNames = new Dictionary<int, string>();

        if (categoryById.TryGetValue(keyword.CategoryId, out var current))
        {
            ancestorNames[current.Level] = current.Name;

            while (current.ParentId.HasValue && categoryById.TryGetValue(current.ParentId.Value, out var parent))
            {
                ancestorNames[parent.Level] = parent.Name;
                current = parent;
            }
        }

        string level1 = ancestorNames.GetValueOrDefault(1, string.Empty).ToTitleCase();
        var level2 = ancestorNames.TryGetValue(2, out string? level2Name) ? Option.Some(level2Name.ToTitleCase()) : Option.None<string>();
        var level3 = ancestorNames.TryGetValue(3, out string? level3Name) ? Option.Some(level3Name.ToTitleCase()) : Option.None<string>();

        return new KeywordMapping(keyword.Keyword.ToTitleCase(), level1, level2, level3, false);
    }
}
