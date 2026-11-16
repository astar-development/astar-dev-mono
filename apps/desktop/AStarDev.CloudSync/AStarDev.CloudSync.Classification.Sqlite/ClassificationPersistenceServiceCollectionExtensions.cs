using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Registers the SQLite classification module.</summary>
public static class ClassificationPersistenceServiceCollectionExtensions
{
    /// <summary>Registers the classification tables in the same SQLite file as the accounts and sync tables, their migration, the <see cref="ITermStore"/>, <see cref="ICategoryStore"/>, <see cref="IFileClassificationStore"/>, <see cref="ISeedStateStore"/> and <see cref="IClassifiableFileSource"/>. The migration is added to the registered <see cref="ICloudSyncDatabase"/>, which runs it after the migrations registered before, so register the accounts and sync persistence first.</summary>
    /// <param name="services">The service collection to register the module with.</param>
    /// <param name="databaseFile">Resolves the SQLite database file; it must be the file the accounts persistence uses.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddClassificationPersistence(this IServiceCollection services, Func<IServiceProvider, IFileInfo> databaseFile) =>
        services
            .AddSingleton<ICloudSyncDatabase, ClassificationDatabase>()
            .AddSingleton<ITermStore, SqliteTermStore>()
            .AddSingleton<ICategoryStore, SqliteCategoryStore>()
            .AddSingleton<IFileClassificationStore, SqliteFileClassificationStore>()
            .AddSingleton<ISeedStateStore, SqliteSeedStateStore>()
            .AddSingleton<IClassifiableFileSource, SqliteClassifiableFileSource>();
}
