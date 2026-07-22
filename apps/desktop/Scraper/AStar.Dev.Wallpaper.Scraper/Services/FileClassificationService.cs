using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public sealed class FileClassificationService(IDbContextFactory<AppDbContext> contextFactory)
{
    public async Task<PageClassificationData> LoadPageClassificationDataAsync(string categoryId, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var searchableCategories = await context.FileClassificationCategories
            .Where(c => c.IncludeInSearch)
            .ToListAsync(token)
            .ConfigureAwait(false);

        var categoryIds = searchableCategories.Select(c => c.Id).ToList();
        var keywordsByCategory = await context.FileClassificationKeywords
            .Where(k => categoryIds.Contains(k.CategoryId))
            .ToListAsync(token)
            .ConfigureAwait(false);

        var searchable = searchableCategories
            .Select(category => (category, (IReadOnlyList<string>)[.. keywordsByCategory.Where(k => k.CategoryId == category.Id).Select(k => k.Keyword)]))
            .ToList();

        var categoryClassification = await ResolveCategoryClassificationAsync(context, categoryId, token).ConfigureAwait(false);

        var includedTags = await context.FileClassificationCategories
            .Where(t => t.IncludeInSearch)
            .ToListAsync(token)
            .ConfigureAwait(false);

        return PageClassificationDataFactory.Create(searchable, categoryClassification, includedTags);
    }

    public async Task<Result<Unit, ScrapeError>> ClassifyAsync(FileDetailEntity fileDetail, PageClassificationData pageData, IReadOnlyList<TagData> imageTags, CancellationToken token)
        => (await Try.RunAsync(() => ClassifyInternalAsync(fileDetail, pageData, imageTags, token)).ConfigureAwait(false))
            .ToResult<Unit, ScrapeError>(exception => ScrapeErrorFactory.CreateClassificationFailed(fileDetail.FileName.Value, exception.Message));

    private async Task<Unit> ClassifyInternalAsync(FileDetailEntity fileDetail, PageClassificationData pageData, IReadOnlyList<TagData> imageTags, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        if (await context.FileClassifications.AnyAsync(classification => classification.FileDetailId == fileDetail.Id, token).ConfigureAwait(false))
            return Unit.Instance;

        var matched = new List<FileClassificationCategoryEntity>(ClassificationMatcher.Match(pageData, fileDetail));
        await CollectTagMatchesAsync(context, pageData.IncludedTags, imageTags, matched, token).ConfigureAwait(false);

        var distinct = matched.DistinctBy(c => c.Name).ToList();

        foreach (var classification in distinct)
        {
            var trackedClassification = EnsureTracked(context, classification);
            context.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, Category = trackedClassification });
        }

        await context.SaveChangesAsync(token).ConfigureAwait(false);

        return Unit.Instance;
    }

    private static FileClassificationCategoryEntity EnsureTracked(AppDbContext context, FileClassificationCategoryEntity classification)
    {
        if (classification.Id != 0)
        {
            var tracked = context.ChangeTracker.Entries<FileClassificationCategoryEntity>()
                .Select(entry => entry.Entity)
                .FirstOrDefault(existing => existing.Id == classification.Id);

            if (tracked is not null)
                return tracked;
        }

        var entry = context.Entry(classification);
        if (entry.State == EntityState.Detached && classification.Id != 0)
            entry.State = EntityState.Unchanged;

        return classification;
    }

    private static async Task<FileClassificationCategoryEntity?> ResolveCategoryClassificationAsync(AppDbContext context, string categoryId, CancellationToken token)
    {
        if (string.IsNullOrEmpty(categoryId)) return null;

        var searchConfig = await context.SearchConfigurations
            .Include(sc => sc.SearchCategories)
            .OrderByDescending(sc => sc.Id)
            .FirstOrDefaultAsync(token)
            .ConfigureAwait(false);

        if (searchConfig is null) return null;

        var category = searchConfig.SearchCategories.FirstOrDefault(c => c.Id == categoryId && c.IncludeInSearch);
        if (category is null) return null;

        var classification = await FindOrCreateClassificationAsync(context, new TagData(category.Name, category.Name), token).ConfigureAwait(false);
        await context.SaveChangesAsync(token).ConfigureAwait(false);

        return classification;
    }

    private static async Task CollectTagMatchesAsync(AppDbContext context, IReadOnlyList<FileClassificationCategoryEntity> includedTags, IReadOnlyList<TagData> imageTags, List<FileClassificationCategoryEntity> matched, CancellationToken token)
    {
        if (imageTags.Count == 0) return;

        var tagSet = new HashSet<string>(imageTags.Select(t => t.Tag), StringComparer.OrdinalIgnoreCase);

        foreach (var tag in includedTags.Where(t => tagSet.Contains(t.Name)))
            matched.Add(await FindOrCreateClassificationAsync(context, new TagData(tag.Name, tag.Name), token).ConfigureAwait(false));
    }

    private static async Task<FileClassificationCategoryEntity> FindOrCreateClassificationAsync(AppDbContext context, TagData name, CancellationToken token)
    {
        string normalizedName = name.Tag.ToTitleCase();

        var tracked = context.ChangeTracker.Entries<FileClassificationCategoryEntity>()
            .Select(e => e.Entity)
            .Where(e => e.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Level)
            .FirstOrDefault();
        if (tracked is not null) return tracked;

        var existing = await context.FileClassificationCategories
            .Where(c => EF.Functions.Collate(c.Name, "NOCASE") == normalizedName)
            .OrderByDescending(c => c.Level)
            .ThenBy(c => c.Id)
            .FirstOrDefaultAsync(token)
            .ConfigureAwait(false);
        if (existing is not null) return existing;

        var root = await FindOrCreateUnclassifiedRootAsync(context, token).ConfigureAwait(false);
        var created = new FileClassificationCategoryEntity { Name = normalizedName, Level = 2, Parent = root };
        context.FileClassificationCategories.Add(created);

        return created;
    }

    private static async Task<FileClassificationCategoryEntity> FindOrCreateUnclassifiedRootAsync(AppDbContext context, CancellationToken token)
    {
        const string rootName = "Unclassified";

        var tracked = context.ChangeTracker.Entries<FileClassificationCategoryEntity>()
            .Select(e => e.Entity)
            .FirstOrDefault(e => e.Level == 1 && e.Name.Equals(rootName, StringComparison.OrdinalIgnoreCase));
        if (tracked is not null) return tracked;

        var existing = await context.FileClassificationCategories
            .FirstOrDefaultAsync(c => c.Level == 1 && EF.Functions.Collate(c.Name, "NOCASE") == rootName, token)
            .ConfigureAwait(false);
        if (existing is not null) return existing;

        var created = new FileClassificationCategoryEntity { Name = rootName, Level = 1 };
        context.FileClassificationCategories.Add(created);

        return created;
    }
}
