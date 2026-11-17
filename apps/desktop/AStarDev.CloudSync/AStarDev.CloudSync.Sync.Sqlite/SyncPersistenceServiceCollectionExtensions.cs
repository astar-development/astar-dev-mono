using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.DependencyInjection;
using AStarDev.EFCoreSqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Registers the SQLite sync module.</summary>
public static class SyncPersistenceServiceCollectionExtensions
{
    /// <summary>Registers the sync tables in the same SQLite file as the accounts, their migration, the <see cref="ISyncRootPolicy"/>, <see cref="ISyncRootReader"/>, <see cref="ISyncRootWriter"/>, the <see cref="ISelectionRuleReader"/>, <see cref="ISelectionRuleWriter"/>, the <see cref="IRemoteIndexReader"/>, <see cref="IRemoteIndexWriter"/>, the <see cref="ISyncBaselineReader"/>, <see cref="ISyncBaselineWriter"/>, <see cref="ISyncBaselineCounter"/>, the <see cref="IUploadRecorder"/>, the <see cref="ISyncAttentionReader"/>, <see cref="ISyncAttentionWriter"/>, <see cref="ISyncAttentionCloser"/>, the <see cref="IAccountPauseReader"/>, <see cref="IAccountPauseWriter"/>, the <see cref="ITransferConcurrencyReader"/>, <see cref="ITransferConcurrencyWriter"/>, the <see cref="IAccountLogLevelSettings"/>, the <see cref="ISyncScheduleSettings"/>, the <see cref="ITrashRetentionSettings"/>, the <see cref="IActivityLogReader"/>, <see cref="IActivityLogWriter"/>, the <see cref="IThroughputReader"/>, <see cref="IThroughputWriter"/>, the <see cref="ISyncRunJournal"/>, the <see cref="IUploadSessionStore"/>, the <see cref="IDatabaseHealthCheck"/>, the host <see cref="IFileSystemCaseRule"/> if no rule is registered and the <see cref="IAccountRemovalParticipant"/> that deletes an account's sync data. The sync migration is added to the registered <see cref="ICloudSyncDatabase"/>, which runs it after the accounts migration, so register the accounts persistence first. Needs an <see cref="IFileSystem"/> and an <see cref="IHomeFolder"/>.</summary>
    /// <param name="services">The service collection to register the module with.</param>
    /// <param name="databaseFile">Resolves the SQLite database file; it must be the file the accounts persistence uses.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddSyncPersistence(this IServiceCollection services, Func<IServiceProvider, IFileInfo> databaseFile)
    {
        var accountsDatabase = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(ICloudSyncDatabase));
        if (accountsDatabase is not null)
            services.Remove(accountsDatabase);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<AccountLogLevels>();
        services.TryAddSingleton<IFileSystemCaseRule, SystemFileSystemCaseRule>();

        return services
            .AddDbContextFactory<SyncDbContext>((provider, options) => options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = databaseFile(provider).FullName }.ToString(), sqlite => sqlite.MigrationsHistoryTable(SyncDbContext.MigrationsHistoryTable)).AddInterceptors(SqliteBusyTimeoutInterceptor.Instance))
            .AddSingleton<ICloudSyncDatabase>(provider => Compose(provider, accountsDatabase))
            .AddSingleton<IDatabaseHealthCheck>(provider => new SqliteDatabaseHealthCheck(databaseFile(provider), provider.GetRequiredService<ILogger<SqliteDatabaseHealthCheck>>()))
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
            .AddSingleton<ISyncBaselineCounter>(provider => provider.GetRequiredService<SqliteSyncBaselineStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteSyncBaselineStore>())
            .AddSingleton<SqliteSyncAttentionStore>()
            .AddSingleton<ISyncAttentionReader>(provider => provider.GetRequiredService<SqliteSyncAttentionStore>())
            .AddSingleton<ISyncAttentionWriter>(provider => provider.GetRequiredService<SqliteSyncAttentionStore>())
            .AddSingleton<ISyncAttentionCloser>(provider => provider.GetRequiredService<SqliteSyncAttentionStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteSyncAttentionStore>())
            .AddSingleton<ISyncScheduleSettings, SqliteSyncScheduleStore>()
            .AddSingleton<ITrashRetentionSettings, SqliteTrashRetentionStore>()
            .AddSingleton<SqliteAccountPauseStore>()
            .AddSingleton<IAccountPauseReader>(provider => provider.GetRequiredService<SqliteAccountPauseStore>())
            .AddSingleton<IAccountPauseWriter>(provider => provider.GetRequiredService<SqliteAccountPauseStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteAccountPauseStore>())
            .AddSingleton<SqliteTransferConcurrencyStore>()
            .AddSingleton<ITransferConcurrencyReader>(provider => provider.GetRequiredService<SqliteTransferConcurrencyStore>())
            .AddSingleton<ITransferConcurrencyWriter>(provider => provider.GetRequiredService<SqliteTransferConcurrencyStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteTransferConcurrencyStore>())
            .AddSingleton<SqliteAccountLogLevelStore>()
            .AddSingleton<IAccountLogLevelSettings>(provider => provider.GetRequiredService<SqliteAccountLogLevelStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteAccountLogLevelStore>())
            .AddSingleton<SqliteThroughputStore>()
            .AddSingleton<IThroughputReader>(provider => provider.GetRequiredService<SqliteThroughputStore>())
            .AddSingleton<IThroughputWriter>(provider => provider.GetRequiredService<SqliteThroughputStore>())
            .AddSingleton<SqliteActivityLogStore>()
            .AddSingleton<IActivityLogReader>(provider => provider.GetRequiredService<SqliteActivityLogStore>())
            .AddSingleton<IActivityLogWriter>(provider => provider.GetRequiredService<SqliteActivityLogStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteActivityLogStore>())
            .AddSingleton<SqliteSyncRunJournal>()
            .AddSingleton<ISyncRunJournal>(provider => provider.GetRequiredService<SqliteSyncRunJournal>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteSyncRunJournal>())
            .AddSingleton<SqliteUploadSessionStore>()
            .AddSingleton<IUploadSessionStore>(provider => provider.GetRequiredService<SqliteUploadSessionStore>())
            .AddSingleton<IAccountRemovalParticipant>(provider => provider.GetRequiredService<SqliteUploadSessionStore>());
    }

    private static ICloudSyncDatabase Compose(IServiceProvider provider, ServiceDescriptor? accountsDatabase)
    {
        var syncDatabase = new SqliteSyncDatabase(provider.GetRequiredService<IDbContextFactory<SyncDbContext>>(), provider.GetRequiredService<ILogger<SqliteSyncDatabase>>());

        return accountsDatabase is null ? syncDatabase : new CompositeCloudSyncDatabase(accountsDatabase.CreateInstance<ICloudSyncDatabase>(provider), syncDatabase);
    }
}
