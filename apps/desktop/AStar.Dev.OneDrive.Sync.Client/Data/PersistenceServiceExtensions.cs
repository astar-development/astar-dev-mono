using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.DataMigration;
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
        _ = services.AddSingleton<IFileClassificationRepository, FileClassificationRepository>();
        _ = services.AddTransient<ICategoryResolutionService, CategoryResolutionService>();
        _ = services.AddTransient<IClassificationDataMigrationService, ClassificationDataMigrationService>();

        return services;
    }

    private static void ConfigureDbContext(DbContextOptionsBuilder builder)
    {
        string dbPath = ApplicationMetadata.ApplicationNameHyphenated.ApplicationDirectory().CombinePath($"{ApplicationMetadata.ApplicationNameHyphenated}.db");
        _ = builder.UseSqlite($"Data Source={dbPath}");
    }
}
