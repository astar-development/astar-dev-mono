using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MelILogger = Microsoft.Extensions.Logging.ILogger;

namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public sealed partial class DatabaseMigrationStartupTask(IServiceProvider services, ILogger<DatabaseMigrationStartupTask> logger, IFileSystem fileSystem, ISpecialFolderResolver folderResolver) : IStartupTask
{
    public const string TaskName = "DatabaseMigration";

    private const string DatabaseFileName    = "file-data.db";
    private const string LegacyAppDataFolder = "AStar.Dev.OneDriveSync";

    string IStartupTask.Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        await using var scope       = services.CreateAsyncScope();
        var             pathProvider = scope.ServiceProvider.GetRequiredService<IAppDataPathProvider>();
        var             dbContext    = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        MigrateFromLegacyPathIfNeeded(pathProvider);

        try
        {
            await dbContext.Database.MigrateAsync(ct).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        LogMigrationCompleted(logger);

        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL", ct).ConfigureAwait(false);
        LogWalModeEnabled(logger);
    }

    private void MigrateFromLegacyPathIfNeeded(IAppDataPathProvider pathProvider)
    {
        string newDbPath = fileSystem.Path.Combine(pathProvider.AppDataDirectory, DatabaseFileName);

        if (fileSystem.File.Exists(newDbPath))
            return;

        string legacyDirectory = fileSystem.Path.Combine(
            folderResolver.GetLocalApplicationDataPath(),
            LegacyAppDataFolder);
        string legacyDbPath = fileSystem.Path.Combine(legacyDirectory, DatabaseFileName);

        if (!fileSystem.File.Exists(legacyDbPath))
            return;

        _ = fileSystem.Directory.CreateDirectory(pathProvider.AppDataDirectory);
        fileSystem.File.Copy(legacyDbPath, newDbPath);
        LogLegacyDatabaseMigrated(logger, legacyDbPath, newDbPath);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database migration completed successfully")]
    private static partial void LogMigrationCompleted(MelILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "WAL journal mode enabled")]
    private static partial void LogWalModeEnabled(MelILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Legacy database copied from '{LegacyPath}' to '{NewPath}'")]
    private static partial void LogLegacyDatabaseMigrated(MelILogger logger, string legacyPath, string newPath);
}
