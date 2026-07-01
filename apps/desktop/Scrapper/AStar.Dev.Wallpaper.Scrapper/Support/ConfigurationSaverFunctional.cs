using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public sealed class ConfigurationSaverFunctional(ScrapeConfiguration scrapeConfiguration, Logger logger, IDbContextFactory<FilesContext> contextFactory)
{
    public async Task SaveUpdatedConfigurationAsync()
    {
        try
        {
            await UpdateAndSaveTheConfigurationAsync();
        }
        catch(Exception exception)
        {
            logger.Error(exception.GetBaseException().Message);
            throw;
        }
    }

    private async Task UpdateAndSaveTheConfigurationAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var entity = await dbContext.ScrapeConfiguration
                                    .Include(e => e.SearchConfiguration)
                                    .ThenInclude(sc => sc.SearchCategories)
                                    .SingleAsync();

        var dedupedCategories = scrapeConfiguration.SearchConfiguration.SearchCategories
                                                   .DistinctBy(c => c.Id)
                                                   .ToList();

        foreach(var cat in dedupedCategories)
        {
            var existing = entity.SearchConfiguration.SearchCategories
                                 .FirstOrDefault(ec => ec.Id == cat.Id);
            if(existing != null)
            {
                existing.LastKnownImageCount = cat.LastKnownImageCount;
                existing.LastPageVisited     = cat.LastPageVisited;
                existing.TotalPages          = cat.TotalPages;
            }
            else
            {
                entity.SearchConfiguration.SearchCategories.Add(new SearchCategories
                {
                    SearchConfigurationId = entity.SearchConfiguration.Id,
                    Id                    = cat.Id,
                    Name                  = cat.Name,
                    LastKnownImageCount   = cat.LastKnownImageCount,
                    LastPageVisited       = cat.LastPageVisited,
                    TotalPages            = cat.TotalPages,
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
