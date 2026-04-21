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
    ///     Adds <see cref="IDbContextFactory{AppDbContext}" /> and all repository services. Database migration is deferred to bootstrap.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        _ = services.AddDbContextFactory<AppDbContext>(ConfigureDbContext, ServiceLifetime.Singleton);
        _ = services.AddSingleton<IAccountRepository, AccountRepository>();
        _ = services.AddSingleton<ISyncRepository, SyncRepository>();
        _ = services.AddSingleton<IDriveStateRepository, DriveStateRepository>();
        _ = services.AddSingleton<ISyncRuleRepository, SyncRuleRepository>();
        _ = services.AddSingleton<ISyncedItemRepository, SyncedItemRepository>();

        return services;
    }

    private static void ConfigureDbContext(DbContextOptionsBuilder builder)
    {
        string dbPath = ApplicationMetadata.ApplicationNameLowered.ApplicationDirectory().CombinePath($"{ApplicationMetadata.ApplicationNameLowered}.db");
        _ = builder.UseSqlite($"Data Source={dbPath}");
    }
}
