using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.DependencyInjection;
using AStarDev.EFCoreSqlite;
using AStarDev.FunctionalParadigm;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Registers the SQLite classification module.</summary>
public static class ClassificationPersistenceServiceCollectionExtensions
{
    /// <summary>Registers the classification tables in the same SQLite file as the accounts and sync tables, their migration, the <see cref="ITermStore"/>, <see cref="ICategoryStore"/>, <see cref="IFileClassificationStore"/>, <see cref="ISeedStateStore"/>, the <see cref="IReclassifyRunStore"/>, the <see cref="IClassifiableFileSource"/> and the <see cref="IAccountRemovalParticipant"/> and <see cref="Sync.ISyncRootChangeParticipant"/> that delete an account's file links. The classification migration is added to the registered <see cref="ICloudSyncDatabase"/>, which runs it after the migrations registered before, so register the accounts and sync persistence first. The file source needs the <see cref="IAccountStore"/> and the <see cref="Sync.ISyncBaselineReader"/>.</summary>
    /// <param name="services">The service collection to register the module with.</param>
    /// <param name="databaseFile">Resolves the SQLite database file; it must be the file the accounts persistence uses.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddClassificationPersistence(this IServiceCollection services, Func<IServiceProvider, IFileInfo> databaseFile)
    {
        var previous = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(ICloudSyncDatabase));
        if (previous is not null)
            _ = services.Remove(previous);

        return services
            .AddDbContextFactory<ClassificationDbContext>((provider, options) => options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = databaseFile(provider).FullName }.ToString(), sqlite => sqlite.MigrationsHistoryTable(ClassificationDbContext.MigrationsHistoryTable)).AddInterceptors(SqliteBusyTimeoutInterceptor.Instance))
            .AddSingleton<ICloudSyncDatabase>(provider => new ClassificationDatabase(CreatePrevious(provider, previous), provider.GetRequiredService<IDbContextFactory<ClassificationDbContext>>(), provider.GetRequiredService<ILogger<ClassificationDatabase>>()))
            .AddSingleton<ITermStore, SqliteTermStore>()
            .AddSingleton<ICategoryStore, SqliteCategoryStore>()
            .AddSingleton<SqliteFileClassificationStore>()
            .AddSingleton<IFileClassificationStore>(provider => provider.GetRequiredService<SqliteFileClassificationStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteFileClassificationStore>())
            .AddSingleton<Sync.ISyncRootChangeParticipant>(provider => provider.GetRequiredService<SqliteFileClassificationStore>())
            .AddSingleton<ISeedStateStore, SqliteSeedStateStore>()
            .AddSingleton<IReclassifyRunStore, SqliteReclassifyRunStore>()
            .AddSingleton<IClassifiableFileSource, SqliteClassifiableFileSource>();
    }

    private static Option<ICloudSyncDatabase> CreatePrevious(IServiceProvider provider, ServiceDescriptor? descriptor) =>
        descriptor is null
            ? Option.None<ICloudSyncDatabase>()
            : Option.Some(descriptor.CreateInstance<ICloudSyncDatabase>(provider));
}
