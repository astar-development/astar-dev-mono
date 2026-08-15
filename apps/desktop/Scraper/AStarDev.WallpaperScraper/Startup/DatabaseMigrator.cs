using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using AStarDev.ControlDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.WallpaperScraper.Startup;

/// <summary>Applies pending Entity Framework Core migrations to the application database at startup.</summary>
public static class DatabaseMigrator
{
    /// <summary>Migrates the database, logging and swallowing any failure so that startup can continue.</summary>
    /// <param name="dbContextFactory">The factory used to create the <see cref="ControlDbContext" />.</param>
    /// <param name="logger">The logger used to report migration failures.</param>
    public static async Task MigrateAsync(IDbContextFactory<ControlDbContext> dbContextFactory, ILogger logger) =>
        await Try.RunAsync(() => ApplyPendingMigrationsAsync(dbContextFactory, logger))
            .TapAsync(static _ => { }, exception => LogMigrationFailure(logger, exception));

    private static async Task<UnitFp> ApplyPendingMigrationsAsync(IDbContextFactory<ControlDbContext> dbContextFactory, ILogger logger)
    {
        LogMessage.Information(logger, "Applying pending database migrations");
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();

        return UnitFp.Instance;
    }

    private static void LogMigrationFailure(ILogger logger, Exception exception) =>
        LogMessage.Error(logger, "Database migration failed", exception);
}
