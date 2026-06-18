using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
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

        var entities = await db.FileClassificationCategories.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

        var categories = new List<FileClassificationCategory>(entities.Count);
        foreach (var e in entities)
        {
            var result = FileClassificationCategoryFactory.Create(
                new FileClassificationCategoryId(e.Id),
                e.Name,
                e.Level,
                e.ParentId.HasValue ? Option.Some(new FileClassificationCategoryId(e.ParentId.Value)) : Option.None<FileClassificationCategoryId>());

            _ = result.Match<object?>(
                ok => { categories.Add(ok); return null; },
                err => { OneDriveSyncClientMessages.ClassificationRowSkipped(logger, e.Id, err); return null; });
        }

        return categories.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileClassificationKeywordEntry>> GetKeywordsForCategoryAsync(FileClassificationCategoryId categoryId, CancellationToken cancellationToken = default)
    {
        int rawId = categoryId.Id;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entities = await db.FileClassificationKeywords
            .AsNoTracking()
            .Where(k => k.CategoryId == rawId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities
            .Select(e => new FileClassificationKeywordEntry(e.Id, new FileClassificationKeyword(e.Keyword, Option.Some(e.IsSpecial))))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KeywordMapping>> GetAllKeywordMappingsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var categories = await db.FileClassificationCategories
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var keywords = await db.FileClassificationKeywords
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var categoryById = categories.ToDictionary(c => c.Id);

        return keywords
            .Select(k => BuildKeywordMapping(k, categoryById))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<Result<FileClassificationCategoryId, string>> AddCategoryAsync(FileClassificationCategory category, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = new FileClassificationCategoryEntity
        {
            Name = category.Name.ToTitleCase(),
            Level = category.Level,
            ParentId = category.ParentId.MapOrDefault(pid => (int?)pid.Id, null)
        };

        db.FileClassificationCategories.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Result<FileClassificationCategoryId, string>.Ok(new FileClassificationCategoryId(entity.Id));
    }

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
        entity.ParentId = category.ParentId.MapOrDefault(pid => (int?)pid.Id, null);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Result<FileClassificationCategoryId, string>.Ok(id);
    }

    /// <inheritdoc />
    public async Task DeleteCategoryAsync(FileClassificationCategoryId id, CancellationToken cancellationToken = default)
    {
        int rawId = id.Id;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await db.FileClassificationCategories.FindAsync([rawId], cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return;

        db.FileClassificationCategories.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<int, string>> AddKeywordAsync(FileClassificationCategoryId categoryId, FileClassificationKeyword keyword, CancellationToken cancellationToken = default)
    {
        int rawCategoryId = categoryId.Id;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        bool hasChildren = await db.FileClassificationCategories
            .AnyAsync(c => c.ParentId == rawCategoryId, cancellationToken)
            .ConfigureAwait(false);

        if (hasChildren)
            return new Result<int, string>.Error("Cannot add keyword to a non-leaf category.");

        var entity = new FileClassificationKeywordEntity
        {
            Keyword = keyword.Value.ToTitleCase(),
            CategoryId = rawCategoryId,
            IsSpecial = keyword.IsSpecialOverride.Match(v => v, () => false)
        };

        db.FileClassificationKeywords.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Result<int, string>.Ok(entity.Id);
    }

    /// <inheritdoc />
    public async Task<Result<int, string>> UpdateKeywordAsync(int keywordId, FileClassificationKeyword keyword, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await db.FileClassificationKeywords.FindAsync([keywordId], cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new Result<int, string>.Error("Keyword not found.");

        bool hasChildren = await db.FileClassificationCategories
            .AnyAsync(c => c.ParentId == entity.CategoryId, cancellationToken)
            .ConfigureAwait(false);

        if (hasChildren)
            return new Result<int, string>.Error("Cannot update keyword on a non-leaf category.");

        entity.Keyword = keyword.Value.ToTitleCase();
        entity.IsSpecial = keyword.IsSpecialOverride.Match(v => v, () => false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new Result<int, string>.Ok(entity.Id);
    }

    /// <inheritdoc />
    public async Task DeleteKeywordAsync(int keywordId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await db.FileClassificationKeywords.FindAsync([keywordId], cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return;

        db.FileClassificationKeywords.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        db.FileClassificationKeywords.RemoveRange(db.FileClassificationKeywords);
        db.FileClassificationCategories.RemoveRange(db.FileClassificationCategories);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
