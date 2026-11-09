using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.Sqlite;

/// <summary>Registers the CloudSync database and the stores built on it.</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>Registers the SQLite database at the file <paramref name="databaseFile"/> resolves, the <see cref="ICloudSyncDatabase"/> that migrates it and the <see cref="IAccountStore"/>.</summary>
    /// <param name="services">The service collection to register with.</param>
    /// <param name="databaseFile">Resolves the SQLite database file.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddCloudSyncPersistence(this IServiceCollection services, Func<IServiceProvider, IFileInfo> databaseFile) =>
        services
            .AddDbContextFactory<CloudSyncDbContext>((provider, options) => options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = databaseFile(provider).FullName }.ToString()))
            .AddSingleton<ICloudSyncDatabase>(provider => new SqliteCloudSyncDatabase(provider.GetRequiredService<IDbContextFactory<CloudSyncDbContext>>(), databaseFile(provider), provider.GetRequiredService<ILogger<SqliteCloudSyncDatabase>>()))
            .AddSingleton<IAccountStore, SqliteAccountStore>();
}
