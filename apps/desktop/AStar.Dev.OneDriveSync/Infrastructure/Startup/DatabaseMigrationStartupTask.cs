using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Disambiguate ILogger: both Serilog and Microsoft.Extensions.Logging declare ILogger.
// All usages in this file intend the MEL interface.
using MelILogger = Microsoft.Extensions.Logging.ILogger;

namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public sealed partial class DatabaseMigrationStartupTask(IServiceProvider services, ILogger<DatabaseMigrationStartupTask> logger) : IStartupTask
{
    public const string TaskName = "DatabaseMigration";

    private const string DatabaseFileName    = "file-data.db";
    private const string LegacyAppDataFolder = "AStar.Dev.OneDriveSync";

    string IStartupTask.Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        await using AsyncServiceScope scope       = services.CreateAsyncScope();
        IAppDataPathProvider          pathProvider = scope.ServiceProvider.GetRequiredService<IAppDataPathProvider>();
        AppDbContext                  dbContext    = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        MigrateFromLegacyPathIfNeeded(pathProvider);

        await dbContext.Database.MigrateAsync(ct).ConfigureAwait(false);
        LogMigrationCompleted(logger);

        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", ct).ConfigureAwait(false);
        LogWalModeEnabled(logger);
    }

    private void MigrateFromLegacyPathIfNeeded(IAppDataPathProvider pathProvider)
    {
        var newDbPath = Path.Combine(pathProvider.AppDataDirectory, DatabaseFileName);

        if (File.Exists(newDbPath))
            return;

        var legacyDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LegacyAppDataFolder);
        var legacyDbPath = Path.Combine(legacyDirectory, DatabaseFileName);

        if (!File.Exists(legacyDbPath))
            return;

        _ = Directory.CreateDirectory(pathProvider.AppDataDirectory);
        File.Copy(legacyDbPath, newDbPath);
        LogLegacyDatabaseMigrated(logger, legacyDbPath, newDbPath);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database migration completed successfully")]
    private static partial void LogMigrationCompleted(MelILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "WAL journal mode enabled")]
    private static partial void LogWalModeEnabled(MelILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Legacy database copied from '{LegacyPath}' to '{NewPath}'")]
    private static partial void LogLegacyDatabaseMigrated(MelILogger logger, string legacyPath, string newPath);
}
