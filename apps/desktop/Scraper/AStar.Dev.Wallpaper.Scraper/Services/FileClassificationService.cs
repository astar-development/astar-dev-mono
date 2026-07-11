using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public sealed class FileClassificationService(IDbContextFactory<AppDbContext> contextFactory, Logger logger)
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

    internal async Task<(List<FileClassificationCategoryEntity> Categories, List<FileClassificationKeywordEntity> Keywords)> ExportClassificationsAsync(CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var categories = await context.FileClassificationCategories.ToListAsync(token).ConfigureAwait(false);
        var keywords = await context.FileClassificationKeywords.ToListAsync(token).ConfigureAwait(false);

        return (categories, keywords);
    }

    internal async Task<Result<Unit, ScrapeError>> ImportClassificationsAsync((List<FileClassificationCategoryEntity> Categories, List<FileClassificationKeywordEntity> Keywords) classifications, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        return await ImportLevelAsync(context, 1, classifications, token)
            .BindAsync(_ => ImportLevelAsync(context, 2, classifications, token))
            .BindAsync(_ => ImportLevelAsync(context, 3, classifications, token))
            .ConfigureAwait(false);
    }

    private async Task<Result<Unit, ScrapeError>> ImportLevelAsync(AppDbContext context, int level, (List<FileClassificationCategoryEntity> Categories, List<FileClassificationKeywordEntity> Keywords) classifications, CancellationToken token)
    {
        var categoriesInLevel = classifications.Categories.Where(c => c.Level == level).OrderBy(c => c.ParentId).ThenBy(c => c.Name);

        foreach (var category in categoriesInLevel)
            await ImportCategoryAsync(context, category, classifications.Keywords, token)
                .TapAsync(onSuccess: _ => { }, onFailure: error => logger.Error("Failed to import classification category: {CategoryName} (Level {Level}). {ErrorMessage}", category.Name, category.Level, error.Message))
                .ConfigureAwait(false);

        return Result.Success<Unit, ScrapeError>(Unit.Value);
    }

    private async Task<Result<Unit, ScrapeError>> ImportCategoryAsync(AppDbContext context, FileClassificationCategoryEntity category, List<FileClassificationKeywordEntity> keywords, CancellationToken token)
    {
        logger.Information("Importing classification category: {CategoryName} (Level {Level})", category.Name, category.Level);

        return await (await UpsertPrimaryAsync(context, category, keywords, token).ConfigureAwait(false))
            .MatchAsync(
                onSuccess: unit => Result.Success<Unit, ScrapeError>(unit),
                onFailure: _ => UpsertFallbackAsync(context, category, keywords, token))
            .ConfigureAwait(false);
    }

    private static async Task<Result<Unit, ScrapeError>> UpsertPrimaryAsync(AppDbContext context, FileClassificationCategoryEntity category, List<FileClassificationKeywordEntity> keywords, CancellationToken token)
        => (await Try.RunAsync(() => SavePrimaryAsync(context, category, keywords, token)).ConfigureAwait(false))
            .ToResult(exception => (ScrapeError)ScrapeErrorFactory.CreateRepositoryOperationFailed(nameof(UpsertPrimaryAsync), exception.Message))
            .Tap(onSuccess: _ => { }, onFailure: _ => DetachUnsavedEntries(context));

    private static async Task<Unit> SavePrimaryAsync(AppDbContext context, FileClassificationCategoryEntity category, List<FileClassificationKeywordEntity> keywords, CancellationToken token)
    {
        var target = await FindExistingCategoryAsync(context, category, token).ConfigureAwait(false);

        if (target is null)
        {
            target = new FileClassificationCategoryEntity
            {
                Id = category.Id,
                Name = category.Name,
                Level = category.Level,
                ParentId = category.ParentId,
                IsFamous = category.IsFamous,
                IsInternet = category.IsInternet,
                IncludeInSearch = category.IncludeInSearch
            };
            context.FileClassificationCategories.Add(target);
        }
        else
        {
            target.IsFamous = category.IsFamous;
            target.IsInternet = category.IsInternet;
            target.IncludeInSearch = category.IncludeInSearch;
        }

        await AddMissingKeywordsAsync(context, target.Id, category.Id, keywords, token).ConfigureAwait(false);
        await context.SaveChangesAsync(token).ConfigureAwait(false);

        return Unit.Value;
    }

    private static async Task<Result<Unit, ScrapeError>> UpsertFallbackAsync(AppDbContext context, FileClassificationCategoryEntity category, List<FileClassificationKeywordEntity> keywords, CancellationToken token)
        => (await Try.RunAsync(() => SaveFallbackAsync(context, category, keywords, token)).ConfigureAwait(false))
            .ToResult(exception => (ScrapeError)ScrapeErrorFactory.CreateRepositoryOperationFailed(nameof(UpsertFallbackAsync), exception.Message))
            .Tap(onSuccess: _ => { }, onFailure: _ => DetachUnsavedEntries(context));

    private static void DetachUnsavedEntries(AppDbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added).ToList())
            entry.State = EntityState.Detached;
    }

    private static async Task<Unit> SaveFallbackAsync(AppDbContext context, FileClassificationCategoryEntity category, List<FileClassificationKeywordEntity> keywords, CancellationToken token)
    {
        var target = await FindExistingCategoryAsync(context, category, token).ConfigureAwait(false);
        int unclassifiedId = await context.FileClassificationCategories
            .Where(c => c.Level == 1 && c.Name == "Unclassified")
            .Select(c => c.Id)
            .FirstOrDefaultAsync(token)
            .ConfigureAwait(false);

        if (target is null)
        {
            target = new FileClassificationCategoryEntity
            {
                Id = category.Id,
                Name = category.Name,
                Level = category.Level,
                ParentId = unclassifiedId,
                IsFamous = category.IsFamous,
                IncludeInSearch = category.IncludeInSearch
            };
            context.FileClassificationCategories.Add(target);
        }
        else
        {
            target.IsFamous = category.IsFamous;
            target.IncludeInSearch = category.IncludeInSearch;
        }

        await AddMissingKeywordsAsync(context, target.Id, category.Id, keywords, token).ConfigureAwait(false);
        await context.SaveChangesAsync(token).ConfigureAwait(false);

        return Unit.Value;
    }

    private static Task<FileClassificationCategoryEntity?> FindExistingCategoryAsync(AppDbContext context, FileClassificationCategoryEntity category, CancellationToken token)
        => context.FileClassificationCategories
            .FirstOrDefaultAsync(c => c.Name == category.Name && c.Level == category.Level && c.ParentId == category.ParentId, token);

    private static async Task AddMissingKeywordsAsync(AppDbContext context, int targetId, int categoryId, List<FileClassificationKeywordEntity> keywords, CancellationToken token)
    {
        var existingKeywords = await context.FileClassificationKeywords
            .Where(k => k.CategoryId == targetId)
            .Select(k => k.Keyword)
            .ToListAsync(token)
            .ConfigureAwait(false);

        foreach (var keyword in keywords.Where(k => k.CategoryId == categoryId))
            if (!existingKeywords.Any(ek => ek.Equals(keyword.Keyword, StringComparison.OrdinalIgnoreCase)))
                context.FileClassificationKeywords.Add(new FileClassificationKeywordEntity { Keyword = keyword.Keyword, CategoryId = targetId });
    }

    private async Task<Unit> ClassifyInternalAsync(FileDetailEntity fileDetail, PageClassificationData pageData, IReadOnlyList<TagData> imageTags, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        if (await context.FileClassifications.AnyAsync(classification => classification.FileDetailId == fileDetail.Id, token).ConfigureAwait(false))
            return Unit.Value;

        var matched = new List<FileClassificationCategoryEntity>(ClassificationMatcher.Match(pageData, fileDetail));
        await CollectTagMatchesAsync(context, pageData.IncludedTags, imageTags, matched, token).ConfigureAwait(false);

        var distinct = matched.DistinctBy(c => c.Name).ToList();

        foreach (var classification in distinct)
        {
            var trackedClassification = EnsureTracked(context, classification);
            context.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, Category = trackedClassification });
        }

        await context.SaveChangesAsync(token).ConfigureAwait(false);

        return Unit.Value;
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
