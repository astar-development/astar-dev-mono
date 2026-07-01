using AStar.Dev.Infrastructure.FilesDb.Data;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public class DatabaseInitializationService(IDbContextFactory<FilesContext> contextFactory, Logger logger)
{
    public async Task InitialiseAsync()
    {
        await using var context = contextFactory.CreateDbContext();

        await context.Database.MigrateAsync();

        await DataSeed.SeedTagsToIgnoreAsync(logger, context);

        var csvPath = Path.Combine(ApplicationMetadata.ApplicationFolder, "Mappings.csv");
        await DataSeed.SeedFileClassificationsAsync(csvPath, logger, context);
    }
}
