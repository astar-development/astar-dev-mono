using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public sealed class FileClassificationImportExportService(IDbContextFactory<AppDbContext> contextFactory, Logger logger)
{
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
}
