using AStar.Dev.Functional.Extensions;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using AStar.Dev.Infrastructure.AppDb;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

/// <inheritdoc />
public sealed class FileClassificationRepository(IDbContextFactory<AppDbContext> dbFactory, ILogger<FileClassificationRepository> logger) : IFileClassificationRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<FileClassificationCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await db.FileClassificationCategories.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

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

            _ = result.Match<object?>(
                ok => { categories.Add(ok); return null; },
                err => { OneDriveSyncClientMessages.ClassificationRowSkipped(logger, e.Id, err); return null; });
        }

        return categories.AsReadOnly();
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<FileClassificationCategoryEntity>> GetAllCategoriesSimpleAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await db.FileClassificationCategories.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

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
                    ParentId = category.ParentId.MapOrDefault(pid => (int?)pid.Id, null)
                };

                db.FileClassificationCategories.Add(entity);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return new FileClassificationCategoryId(entity.Id);
            }).MapFailureAsync(ex => ex.GetBaseException().Message);

    /// <inheritdoc />
    public async Task<Result<FileClassificationCategoryId, string>> UpdateCategoryAsync(FileClassificationCategoryId id, FileClassificationCategory category, CancellationToken cancellationToken = default)
    {
        int rawId = id.Id;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await db.FileClassificationCategories.FindAsync([rawId], cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new Result<FileClassificationCategoryId, string>.Error("Category not found.");

        entity.Name = category.Name.ToTitleCase();
        entity.Level = category.Level;
        entity.IsFamous = category.IsFamous;
        entity.IsInternet = category.IsInternet;
        entity.ParentId = category.ParentId.MapOrDefault(pid => (int?)pid.Id, null);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Result<FileClassificationCategoryId, string>.Ok(id);
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
