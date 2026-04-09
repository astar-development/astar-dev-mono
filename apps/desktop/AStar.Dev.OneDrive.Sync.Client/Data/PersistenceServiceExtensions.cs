using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure;
using AStar.Dev.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Data;

/// <summary>
///     Registers all persistence-layer services with the DI container.
/// </summary>
internal static class PersistenceServiceExtensions
{
    /// <summary>
    ///     Adds <see cref="AppDbContext" />, <see cref="IApplicationPathsProvider" />,
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        var db = DbContextFactory.Create();
        db.Database.Migrate();
        IAccountRepository accountRepository = new AccountRepository(db);
        ISyncRepository syncRepository    = new SyncRepository(db);
        _ = services.AddDbContext<AppDbContext>(ConfigureOptions);
        _ = services.AddDbContextFactory<AppDbContext>(ConfigureOptions);
        _ = services.AddSingleton(accountRepository);
        _ = services.AddSingleton(syncRepository);

        return services;
    }

    private static void ConfigureOptions(IServiceProvider sp, DbContextOptionsBuilder options)
    {
        string dbPath = ApplicationMetadata.ApplicationNameLowered.ApplicationDirectory().CombinePath($"{ApplicationMetadata.ApplicationNameLowered}.db");
        _ = options.UseSqlite($"DataSource={dbPath}");
    }
}
