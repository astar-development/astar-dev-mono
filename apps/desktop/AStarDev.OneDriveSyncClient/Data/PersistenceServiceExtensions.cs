using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure;
using AStarDev.OneDriveSyncClient.Infrastructure.Data;
using AStarDev.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.OneDriveSyncClient.Data;

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
        _ = services.AddSingleton<IFileClassificationRepository, FileClassificationRepository>();
        _ = services.AddSingleton<IFileDetailResolver, FileDetailResolver>();
        _ = services.AddTransient<ICategoryResolutionService, CategoryResolutionService>();

        return services;
    }

    private static void ConfigureDbContext(DbContextOptionsBuilder builder)
    {
        string dbPath = ApplicationMetadata.ApplicationNameHyphenated.ApplicationDirectory().CombinePath($"{ApplicationMetadata.ApplicationNameHyphenated}.db");
        _ = builder.UseSqlite($"Data Source={dbPath}");
    }
}
