using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Persistence;

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
        var pathProvider = sp.GetRequiredService<IApplicationPathsProvider>();
        _ = Directory.CreateDirectory(pathProvider.ApplicationDirectory);
        string dbPath = Path.Combine(pathProvider.ApplicationDirectory, ApplicationMetadata.ApplicationNameLowered);
        _ = options.UseSqlite($"DataSource={dbPath}");
    }
}
