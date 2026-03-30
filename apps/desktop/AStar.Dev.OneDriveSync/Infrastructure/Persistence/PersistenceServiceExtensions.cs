using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Registers all persistence-layer services with the DI container.
/// </summary>
internal static class PersistenceServiceExtensions
{
    /// <summary>
    ///     Adds <see cref="AppDbContext" />, <see cref="IAppDataPathProvider" />, and
    ///     <see cref="IDbBackupService" /> to <paramref name="services" />.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        _ = services.AddSingleton<IAppDataPathProvider, LocalAppDataPathProvider>();
        _ = services.AddSingleton<IDbBackupService, DbBackupService>();

        _ = services.AddDbContext<AppDbContext>((sp, options) =>
        {
            IAppDataPathProvider pathProvider = sp.GetRequiredService<IAppDataPathProvider>();
            _ = Directory.CreateDirectory(pathProvider.AppDataDirectory);
            var dbPath = Path.Combine(pathProvider.AppDataDirectory, "file-data.db");
            _ = options.UseSqlite($"DataSource={dbPath}");
        });

        return services;
    }
}
