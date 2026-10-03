using AStarDev.ControlDb;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.ScraperPlaying.Startup;

/// <summary>Applies pending Entity Framework Core migrations to the application database at startup.</summary>
public static class DatabaseMigrator
{
    /// <summary>Migrates the database and logs any failure before returning it to the startup boundary.</summary>
    /// <param name="dbContextFactory">The factory used to create the <see cref="ControlDbContext" />.</param>
    /// <param name="logger">The logger used to report migration failures.</param>
    public static async Task MigrateAsync(IDbContextFactory<ControlDbContext> dbContextFactory, ILogger logger)
    {
        try
        {
            _ = await ApplyPendingMigrationsAsync(dbContextFactory, logger);
        }
        catch (Exception exception)
        {
            LogMigrationFailure(logger, exception);
            throw;
        }
    }

    private static async Task<Unit> ApplyPendingMigrationsAsync(IDbContextFactory<ControlDbContext> dbContextFactory, ILogger logger)
    {
        LogMessage.Information(logger, "Applying pending database migrations");
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();

        LogMessage.Information(logger, "Database migrations applied successfully");

        return Unit.Instance;
    }

    private static void LogMigrationFailure(ILogger logger, Exception exception) =>
        LogMessage.Error(logger, "Database migration failed", exception);
}
