using System;
using System.IO;
using AStar.Dev.Sync.Engine.Features.StateTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncEngineBackup = AStar.Dev.Sync.Engine.Infrastructure.IDbBackupService;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Registers all persistence-layer services with the DI container.
/// </summary>
internal static class PersistenceServiceExtensions
{
    /// <summary>
    ///     Adds <see cref="AppDbContext" />, <see cref="IAppDataPathProvider" />,
    ///     <see cref="IDbBackupService" />, and <see cref="ISyncStateStore"/> to <paramref name="services" />.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        _ = services.AddSingleton<IAppDataPathProvider, LocalAppDataPathProvider>();
        _ = services.AddSingleton<IDbBackupService, DbBackupService>();
        _ = services.AddSingleton<SyncEngineBackup, SyncEngineDbBackupAdapter>();

        _ = services.AddDbContext<AppDbContext>(ConfigureOptions);
        _ = services.AddDbContextFactory<AppDbContext>(ConfigureOptions);

        _ = services.AddTransient<ISyncStateStore, SqliteSyncStateStore>();

        return services;
    }

    private static void ConfigureOptions(IServiceProvider sp, DbContextOptionsBuilder options)
    {
        var pathProvider = sp.GetRequiredService<IAppDataPathProvider>();
        _ = Directory.CreateDirectory(pathProvider.AppDataDirectory);
        string dbPath = Path.Combine(pathProvider.AppDataDirectory, "file-data.db");
        _ = options.UseSqlite($"DataSource={dbPath}");
    }
}
