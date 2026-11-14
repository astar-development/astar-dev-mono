using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AStarDev.CloudSync.Sync;

/// <summary>Registers the sync module's in-process services.</summary>
public static class SyncServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IAccountSyncLock"/>, <see cref="IAccountThrottleGate"/>, <see cref="ISyncSelectionEvaluator"/>, <see cref="IFolderTreeSource"/>, <see cref="ISyncPairReader"/>, <see cref="IPathQuarantine"/>, <see cref="IConflictClassifier"/>, <see cref="ILocalFolderFilter"/>, <see cref="ILocalFileFilter"/>, <see cref="ILocalChangeDetector"/>, <see cref="IUploadPlanner"/>, <see cref="IUploadPass"/>, <see cref="IDownloadPlanner"/>, <see cref="IContentHasher"/>, <see cref="ILocalFolderBrowser"/>, <see cref="IDefaultSyncRootProvider"/>, <see cref="IDefaultSyncRootAssigner"/> (under the account lease), <see cref="ILeasedSelectionRuleWriter"/>, the keyed <see cref="IProviderCapability{TCapability}"/> resolver and the account services (<see cref="IAccountActions"/>, <see cref="IAccountRemover"/>, <see cref="IAccountSignInStates"/>, <see cref="ISignInStateRefresher"/>, <see cref="IAccountQuotaRefresher"/>) and the sync run services (<see cref="ISyncPassRunner"/>, <see cref="ISyncActivitySource"/>, <see cref="ISyncCancelAction"/>, <see cref="ISyncRunStatusSource"/>, <see cref="ISyncProgressFeed"/>, <see cref="ISyncNowAction"/>, <see cref="IStaggerPolicy"/>, <see cref="ISyncScheduler"/>) as singletons, and <see cref="TimeProvider.System"/> if no clock is registered. Needs an <see cref="ISyncBaselineReader"/>, an <see cref="ISyncBaselineWriter"/>, an <see cref="IRemoteIndexReader"/>, an <see cref="ISyncAttentionReader"/>, an <see cref="ISyncAttentionWriter"/>, an <see cref="ISyncAttentionCloser"/>, an <see cref="IActivityLogWriter"/>, an <c>IAccountStore</c>, an <see cref="IHomeFolder"/>, an <see cref="ISyncRootReader"/>, an <see cref="ISyncRootWriter"/>, an <see cref="ISelectionRuleReader"/>, an <see cref="ISelectionRuleWriter"/>, an <see cref="IUploadRecorder"/>, an <see cref="ISyncRootPolicy"/>, an <see cref="IConfirmSyncRestartDialog"/>, an <see cref="ISyncScheduleSettings"/>, an <see cref="ITrashRetentionSettings"/>, an <see cref="IAccountPauseReader"/> and logging (<c>AddLogging</c>).</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddCloudSync(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(typeof(IProviderCapability<>), typeof(KeyedProviderCapability<>));

        return services
            .AddSingleton<ISyncSelectionEvaluator, SyncSelectionEvaluator>()
            .AddSingleton<IFolderTreeSource, FolderTreeSource>()
            .AddSingleton<ISyncPairReader, SyncPairReader>()
            .AddSingleton<RemoteChangeRelevance>()
            .AddSingleton<ISelectionRuleRelocator, SelectionRuleRelocator>()
            .AddSingleton<IRemoteIndexUpdater, RemoteIndexUpdater>()
            .AddSingleton<ResyncPathQuarantine>()
            .AddSingleton<IPathQuarantine, AttentionPathQuarantine>()
            .AddSingleton<IConflictClassifier, ConflictClassifier>()
            .AddSingleton<ILocalFolderFilter, LocalFolderFilter>()
            .AddSingleton<ILocalFileFilter, LocalFileFilter>()
            .AddSingleton<LocalTreeWalker>()
            .AddSingleton<LocalChangeComparer>()
            .AddSingleton<LocalDeletionFinder>()
            .AddSingleton<LocalDeletionGuard>()
            .AddSingleton<NewFolderRules>()
            .AddSingleton<IUploadPlanner, UploadPlanner>()
            .AddSingleton(UploadPassOptionsFactory.CreateDefault())
            .AddSingleton<UploadPassProviders>()
            .AddSingleton<UploadPassInputs>()
            .AddSingleton<UploadItemExecutor>()
            .AddSingleton<IUploadPass, UploadPass>()
            .AddSingleton<ILocalChangeDetector, LocalChangeDetector>()
            .AddSingleton<IDownloadPlanner, DownloadPlanner>()
            .AddSingleton(DownloadPassOptionsFactory.Create())
            .AddSingleton<LocalTrash>()
            .AddSingleton<ILocalFileOperations, LocalFileOperations>()
            .AddSingleton<IDiskSpaceProbe, DiskSpaceProbe>()
            .AddSingleton<DownloadPlanLoader>()
            .AddSingleton<DownloadFileStep>()
            .AddSingleton<DownloadExecutor>()
            .AddSingleton<DownloadHolds>()
            .AddSingleton<IDownloadPass, DownloadPass>()
            .AddSingleton<IContentHasher, Sha256ContentHasher>()
            .AddSingleton<ILocalFolderBrowser, LocalFolderBrowser>()
            .AddSingleton<IDefaultSyncRootProvider, DefaultSyncRootProvider>()
            .AddSingleton<DefaultSyncRootAssigner>()
            .AddSingleton<IDefaultSyncRootAssigner, LeasedDefaultSyncRootAssigner>()
            .AddSingleton<ILeasedSelectionRuleWriter, LeasedSelectionRuleWriter>()
            .AddSingleton<IAccountSyncLock, InMemoryAccountSyncLock>()
            .AddSingleton<IAccountThrottleGate, AccountThrottleGate>()
            .AddSingleton<IAccountSignInStates, InMemoryAccountSignInStates>()
            .AddSingleton<AccountLeaseWaiter>()
            .AddSingleton<IAccountRemover, AccountRemover>()
            .AddSingleton<IAccountActions, AccountActions>()
            .AddSingleton<ISignInStateRefresher, SignInStateRefresher>()
            .AddSingleton<SyncProgressFeed>()
            .AddSingleton<ISyncProgressFeed>(provider => provider.GetRequiredService<SyncProgressFeed>())
            .AddSingleton<ISyncProgressSink>(provider => provider.GetRequiredService<SyncProgressFeed>())
            .AddSingleton<SyncPassRegistry>()
            .AddSingleton<ISyncActivitySource>(provider => provider.GetRequiredService<SyncPassRegistry>())
            .AddSingleton<ISyncCancelAction>(provider => provider.GetRequiredService<SyncPassRegistry>())
            .AddSingleton<ISyncRunStatusSource>(provider => provider.GetRequiredService<SyncPassRegistry>())
            .AddSingleton<SyncPassAttention>()
            .AddSingleton<IAccountQuotaRefresher, AccountQuotaRefresher>()
            .AddSingleton<SyncPassActivity>()
            .AddSingleton<SyncPassSteps>()
            .AddSingleton<ISyncPassRunner, SyncPassRunner>()
            .AddSingleton<ISyncNowAction, SyncNowAction>()
            .AddSingleton<IStaggerPolicy, StaggerPolicy>()
            .AddSingleton(SyncSchedulerOptionsFactory.CreateDefault())
            .AddSingleton<SyncSchedulerReaders>()
            .AddSingleton<ISyncScheduler, SyncScheduler>();
    }
}
