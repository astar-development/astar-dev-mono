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
        services.AddSingleton<IAppDataPathProvider, LocalAppDataPathProvider>();
        services.AddSingleton<IDbBackupService, DbBackupService>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var pathProvider = sp.GetRequiredService<IAppDataPathProvider>();
            Directory.CreateDirectory(pathProvider.AppDataDirectory);
            var dbPath = Path.Combine(pathProvider.AppDataDirectory, "data.db");
            options.UseSqlite($"DataSource={dbPath}");
        });

        return services;
    }
}
