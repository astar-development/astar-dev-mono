using AStar.Dev.Infrastructure.AppDb;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public class DatabaseInitializationService(IDbContextFactory<AppDbContext> contextFactory, Logger logger)
{
    public async Task InitialiseAsync()
    {
        await using var context = contextFactory.CreateDbContext();

        await context.Database.MigrateAsync();

        await DataSeed.SeedTagsToIgnoreAsync(logger, context);
        await DataSeed.SeedScrapeConfigurationAsync(logger, context);

        string csvPath = Path.Combine(ApplicationMetadata.ApplicationFolder, "Mappings.csv");
        await DataSeed.SeedFileClassificationsAsync(csvPath, logger, context);
    }
}
