using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Registers the SQLite sync module.</summary>
public static class SyncPersistenceServiceCollectionExtensions
{
    /// <summary>Registers the sync tables in the same SQLite file as the accounts, their migration, the <see cref="ISyncRootPolicy"/>, <see cref="ISyncRootReader"/>, <see cref="ISyncRootWriter"/>, the <see cref="ISelectionRuleReader"/>, <see cref="ISelectionRuleWriter"/>, the <see cref="IRemoteIndexReader"/>, <see cref="IRemoteIndexWriter"/>, the <see cref="ISyncBaselineReader"/>, <see cref="ISyncBaselineWriter"/>, the <see cref="IUploadRecorder"/>, the <see cref="ISyncAttentionReader"/>, <see cref="ISyncAttentionWriter"/>, <see cref="ISyncAttentionCloser"/>, the <see cref="ISyncOutcomeReader"/>, <see cref="ISyncOutcomeWriter"/> and the <see cref="IAccountRemovalParticipant"/> that deletes an account's sync data. The sync migration is added to the registered <see cref="ICloudSyncDatabase"/>, which runs it after the accounts migration, so register the accounts persistence first. Needs an <see cref="IFileSystem"/> and an <see cref="IHomeFolder"/>.</summary>
    /// <param name="services">The service collection to register the module with.</param>
    /// <param name="databaseFile">Resolves the SQLite database file; it must be the file the accounts persistence uses.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddSyncPersistence(this IServiceCollection services, Func<IServiceProvider, IFileInfo> databaseFile)
    {
        var accountsDatabase = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(ICloudSyncDatabase));
        if (accountsDatabase is not null)
            services.Remove(accountsDatabase);

        services.TryAddSingleton(TimeProvider.System);

        return services
            .AddDbContextFactory<SyncDbContext>((provider, options) => options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = databaseFile(provider).FullName }.ToString(), sqlite => sqlite.MigrationsHistoryTable(SyncDbContext.MigrationsHistoryTable)))
            .AddSingleton<ICloudSyncDatabase>(provider => Compose(provider, accountsDatabase))
            .AddSingleton<ISyncRootPolicy, SyncRootPolicy>()
            .AddSingleton<SqliteSyncRootStore>()
            .AddSingleton<ISyncRootReader>(provider => provider.GetRequiredService<SqliteSyncRootStore>())
            .AddSingleton<ISyncRootWriter>(provider => provider.GetRequiredService<SqliteSyncRootStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteSyncRootStore>())
            .AddSingleton<SqliteSelectionRuleStore>()
            .AddSingleton<ISelectionRuleReader>(provider => provider.GetRequiredService<SqliteSelectionRuleStore>())
            .AddSingleton<ISelectionRuleWriter>(provider => provider.GetRequiredService<SqliteSelectionRuleStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteSelectionRuleStore>())
            .AddSingleton<SqliteRemoteIndexStore>()
            .AddSingleton<IRemoteIndexReader>(provider => provider.GetRequiredService<SqliteRemoteIndexStore>())
            .AddSingleton<IRemoteIndexWriter>(provider => provider.GetRequiredService<SqliteRemoteIndexStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteRemoteIndexStore>())
            .AddSingleton<IUploadRecorder, SqliteUploadRecorder>()
            .AddSingleton<SqliteSyncBaselineStore>()
            .AddSingleton<ISyncBaselineReader>(provider => provider.GetRequiredService<SqliteSyncBaselineStore>())
            .AddSingleton<ISyncBaselineWriter>(provider => provider.GetRequiredService<SqliteSyncBaselineStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteSyncBaselineStore>())
            .AddSingleton<SqliteSyncAttentionStore>()
            .AddSingleton<ISyncAttentionReader>(provider => provider.GetRequiredService<SqliteSyncAttentionStore>())
            .AddSingleton<ISyncAttentionWriter>(provider => provider.GetRequiredService<SqliteSyncAttentionStore>())
            .AddSingleton<ISyncAttentionCloser>(provider => provider.GetRequiredService<SqliteSyncAttentionStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteSyncAttentionStore>())
            .AddSingleton<SqliteSyncOutcomeStore>()
            .AddSingleton<ISyncOutcomeReader>(provider => provider.GetRequiredService<SqliteSyncOutcomeStore>())
            .AddSingleton<ISyncOutcomeWriter>(provider => provider.GetRequiredService<SqliteSyncOutcomeStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteSyncOutcomeStore>());
    }

    private static ICloudSyncDatabase Compose(IServiceProvider provider, ServiceDescriptor? accountsDatabase)
    {
        var syncDatabase = new SqliteSyncDatabase(provider.GetRequiredService<IDbContextFactory<SyncDbContext>>(), provider.GetRequiredService<ILogger<SqliteSyncDatabase>>());

        return accountsDatabase is null ? syncDatabase : new CompositeCloudSyncDatabase(CreateAccountsDatabase(provider, accountsDatabase), syncDatabase);
    }

    private static ICloudSyncDatabase CreateAccountsDatabase(IServiceProvider provider, ServiceDescriptor descriptor) =>
        descriptor.ImplementationInstance as ICloudSyncDatabase
        ?? descriptor.ImplementationFactory?.Invoke(provider) as ICloudSyncDatabase
        ?? (ICloudSyncDatabase)ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!);
}
