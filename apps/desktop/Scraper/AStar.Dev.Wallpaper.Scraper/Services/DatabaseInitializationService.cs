using AStar.Dev.Infrastructure.AppDb;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public class DatabaseInitializationService(IDbContextFactory<AppDbContext> contextFactory, Logger logger)
{
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        await context.Database.MigrateAsync(cancellationToken);

        await DataSeed.SeedTagsToIgnoreAsync(logger, context, cancellationToken);
        await DataSeed.SeedScrapeConfigurationAsync(logger, context, cancellationToken);

        string csvPath = Path.Combine(ApplicationMetadata.ApplicationFolder, "Mappings.csv");
        await DataSeed.SeedFileClassificationsAsync(csvPath, logger, context, cancellationToken);
    }
}
