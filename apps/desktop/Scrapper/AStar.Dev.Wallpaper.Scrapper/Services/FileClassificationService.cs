using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

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

        var includedTags = await context.ScrapedTags
            .Where(t => t.IncludeInSearch)
            .ToListAsync(token)
            .ConfigureAwait(false);

        return PageClassificationDataFactory.Create(searchable, categoryClassification, includedTags);
    }

    public async Task ClassifyAsync(FileDetailEntity fileDetail, PageClassificationData pageData, IReadOnlyList<string> imageTags, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        if (await context.FileClassifications.AnyAsync(classification => classification.FileDetailId == fileDetail.Id, token).ConfigureAwait(false))
            return;

        var matched = new List<FileClassificationCategoryEntity>();

        CollectFileNameMatches(pageData.SearchableClassifications, fileDetail, matched);
        if (pageData.CategoryClassification is not null)
            matched.Add(pageData.CategoryClassification);
        await CollectTagMatchesAsync(context, pageData.IncludedTags, imageTags, matched, token).ConfigureAwait(false);

        var distinct = matched.DistinctBy(c => c.Name).ToList();

        await context.SaveChangesAsync(token).ConfigureAwait(false);

        foreach (var classification in distinct)
            context.FileClassifications.Add(new FileClassificationEntity
            {
                FileDetailId = fileDetail.Id,
                CategoryId = classification.Id
            });

        await context.SaveChangesAsync(token).ConfigureAwait(false);
    }

    internal async Task<(List<FileClassificationCategoryEntity> Categories, List<FileClassificationKeywordEntity> Keywords)> ExportClassificationsAsync(CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var categories = await context.FileClassificationCategories.ToListAsync(token).ConfigureAwait(false);
        var keywords = await context.FileClassificationKeywords.ToListAsync(token).ConfigureAwait(false);

        return (categories, keywords);
    }

    internal async Task<Unit> ImportClassificationsAsync((List<FileClassificationCategoryEntity> Categories, List<FileClassificationKeywordEntity> Keywords) classifications, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        foreach (var category in classifications.Categories)
        {
            var target = await context.FileClassificationCategories
                .FirstOrDefaultAsync(c => c.Name == category.Name && c.Level == category.Level && c.ParentId == category.ParentId, token)
                .ConfigureAwait(false);

            if (target is null)
            {
                target = new FileClassificationCategoryEntity
                {
                    Name = category.Name,
                    Level = category.Level,
                    ParentId = category.ParentId,
                    IsFamous = category.IsFamous,
                    IncludeInSearch = category.IncludeInSearch
                };
                context.FileClassificationCategories.Add(target);
                await context.SaveChangesAsync(token).ConfigureAwait(false);
            }
            else
            {
                target.IsFamous = category.IsFamous;
                target.IncludeInSearch = category.IncludeInSearch;
            }

            var existingKeywords = await context.FileClassificationKeywords
                .Where(k => k.CategoryId == target.Id)
                .Select(k => k.Keyword)
                .ToListAsync(token)
                .ConfigureAwait(false);

            foreach (var keyword in classifications.Keywords.Where(k => k.CategoryId == category.Id))
                if (!existingKeywords.Any(ek => ek.Equals(keyword.Keyword, StringComparison.OrdinalIgnoreCase)))
                    context.FileClassificationKeywords.Add(new FileClassificationKeywordEntity { Keyword = keyword.Keyword, CategoryId = target.Id });
        }

        await context.SaveChangesAsync(token).ConfigureAwait(false);

        return Unit.Value;
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

        var classification = await FindOrCreateClassificationAsync(context, category.Name, token).ConfigureAwait(false);
        await context.SaveChangesAsync(token).ConfigureAwait(false);

        return classification;
    }

    private static void CollectFileNameMatches(IReadOnlyList<(FileClassificationCategoryEntity Category, IReadOnlyList<string> Keywords)> searchable, FileDetailEntity fileDetail, List<FileClassificationCategoryEntity> matched)
        => matched.AddRange(searchable
            .Where(entry => entry.Keywords.Any(keyword => fileDetail.FullNameWithPath().Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .Select(entry => entry.Category));

    private static async Task CollectTagMatchesAsync(AppDbContext context, IReadOnlyList<ScrapedTagEntity> includedTags, IReadOnlyList<string> imageTags, List<FileClassificationCategoryEntity> matched, CancellationToken token)
    {
        if (imageTags.Count == 0) return;

        var tagSet = new HashSet<string>(imageTags, StringComparer.OrdinalIgnoreCase);

        foreach (var tag in includedTags.Where(t => tagSet.Contains(t.Value)))
            matched.Add(await FindOrCreateClassificationAsync(context, tag.Value, token).ConfigureAwait(false));
    }

    private static async Task<FileClassificationCategoryEntity> FindOrCreateClassificationAsync(AppDbContext context, string name, CancellationToken token)
    {
        string normalizedName = name.ToTitleCase();

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
