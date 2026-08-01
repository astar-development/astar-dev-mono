using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public sealed class ConfigurationSaver(ScrapeConfiguration scrapeConfiguration, Logger logger, IDbContextFactory<AppDbContext> contextFactory)
{
    public async Task<Result<Unit, ScrapeError>> SaveUpdatedConfigurationAsync()
        => (await Try.RunAsync(UpdateAndSaveTheConfigurationAsync).ConfigureAwait(false))
            .Tap(_ => { }, exception => logger.Error(exception.GetBaseException().Message))
            .ToResult<Unit, ScrapeError>(exception => ScrapeErrorFactory.CreateConfigurationSaveFailed(exception.Message));

    private async Task<Unit> UpdateAndSaveTheConfigurationAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var entity = await dbContext.ScrapeConfiguration
                                    .Include(e => e.SearchConfiguration)
                                    .ThenInclude(sc => sc.SearchCategories)
                                    .SingleAsync().ConfigureAwait(false);

        var existingCategories = entity.SearchConfiguration.SearchCategories
                                        .Select(existing => (existing.Id, existing.Name, existing.LastKnownImageCount, existing.LastPageVisited, existing.TotalPages));

        var (toUpdate, toAdd) = CategoryMerger.MergeCategories(existingCategories, scrapeConfiguration.SearchConfiguration.SearchCategories);

        ApplyUpdates(entity, toUpdate);
        ApplyAdditions(entity, toAdd);

        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        return Unit.Value;
    }

    private static void ApplyUpdates(ScrapeConfigurationEntity entity, IReadOnlyList<Category> toUpdate)
    {
        foreach (var category in toUpdate)
        {
            var existing = entity.SearchConfiguration.SearchCategories.First(ec => ec.Id == category.Id);
            existing.LastKnownImageCount = category.LastKnownImageCount;
            existing.LastPageVisited = category.LastPageVisited;
            existing.TotalPages = category.TotalPages;
        }
    }

    private static void ApplyAdditions(ScrapeConfigurationEntity entity, IReadOnlyList<Category> toAdd)
    {
        foreach (var category in toAdd)
            entity.SearchConfiguration.SearchCategories.Add(new SearchCategoryEntity
            {
                SearchConfigurationId = entity.SearchConfiguration.Id,
                Id = category.Id,
                Name = category.Name,
                LastKnownImageCount = category.LastKnownImageCount,
                LastPageVisited = category.LastPageVisited,
                TotalPages = category.TotalPages,
            });
    }
}
