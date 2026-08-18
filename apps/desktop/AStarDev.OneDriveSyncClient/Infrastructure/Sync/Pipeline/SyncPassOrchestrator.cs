using System.Threading.Channels;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;

internal sealed class SyncPassOrchestrator(ISyncPassRepositories syncPassRepositories, SyncServiceDependencies dependencies, IOptions<SyncSettings> syncSettings, ISettingsService settingsService, ILocalizationService localizationService, ILogger<SyncPassOrchestrator> logger) : ISyncPassOrchestrator
{
    public async Task<SyncPassResult> OrchestrateAsync(OneDriveAccount account, AccountSyncConfig syncConfig, Func<CancellationToken, Task<string>> tokenFactory, Func<SyncConflict, Task> conflictCallback, Action<SyncProgressEventArgs>? onProgress = null, Func<JobCompletedEventArgs, Task>? onJobCompleted = null, CancellationToken cancellationToken = default)
    {
        var driveState = (await syncPassRepositories.DriveStateRepository.GetByAccountIdAsync(account.Id, cancellationToken).ConfigureAwait(false))
            .Match(v => v, () => new DriveStateEntity { AccountId = account.Id });

        driveState.LastSyncStartedAt = Option.Some(DateTimeOffset.UtcNow);
        driveState.DeltaLink = Option.None<string>();
        await syncPassRepositories.DriveStateRepository.UpsertAsync(driveState, cancellationToken).ConfigureAwait(false);

        var mappings = await syncPassRepositories.ClassificationRepository.GetAllCategoriesAsync(cancellationToken).ConfigureAwait(false);

        OneDriveSyncClientMessages.SyncPipelinePreparing(logger, account.Id.Value);
        RaiseProgress(account.Id.Value, 0, 0, localizationService.GetLocal("Sync.Preparing"), onProgress);

        int progressReportInterval = syncSettings.Value.ProgressReportInterval;
        int workerCount = settingsService.Current.ConcurrentWorkerCount;
        var context = new RemoteEnumerationContext();

        Action<int>? enumerationProgress = onProgress is null ? null : count =>
        {
            if (count == 1 || count % progressReportInterval == 0)
                RaiseProgress(account.Id.Value, count, 0, localizationService.GetLocal("Sync.Enumerating", count), onProgress);
        };

        Action<string>? stageChanged = onProgress is null ? null : stage => RaiseProgress(account.Id.Value, 0, 0, localizationService.GetLocal(stage), onProgress);

        var jobChannel = Channel.CreateBounded<SyncJob>(new BoundedChannelOptions(workerCount * 4) { FullMode = BoundedChannelFullMode.Wait, SingleReader = false, SingleWriter = true });
        var firstJobSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var producerTask = RunProducerAsync(account, syncConfig, tokenFactory, conflictCallback, enumerationProgress, stageChanged, context, onProgress, jobChannel.Writer, firstJobSignal, mappings, cancellationToken);

        bool hasJobs;
        try
        {
            hasJobs = await firstJobSignal.Task.ConfigureAwait(false);
        }
        catch
        {
            jobChannel.Writer.TryComplete();
            await producerTask.ConfigureAwait(false);
            throw;
        }

        int failedJobCount = 0;
        if (hasJobs)
            failedJobCount = await dependencies.JobExecutor.ExecuteAsync(account, tokenFactory, jobChannel.Reader.ReadAllAsync(cancellationToken), context.SyncedItems, mappings, onProgress ?? (_ => { }), onJobCompleted ?? (_ => Task.CompletedTask), cancellationToken).ConfigureAwait(false);

        await producerTask.ConfigureAwait(false);

        if (context.HadNoRules)
            return SyncPassResultFactory.Create(didRun: false, failedJobCount: 0);

        if (!hasJobs)
            onProgress?.Invoke(new SyncProgressEventArgs(account.Id.Value, string.Empty, 0, 0, localizationService.GetLocal("Sync.NoChanges"), SyncState.Idle));

        await syncPassRepositories.AccountRepository.GetByIdAsync(account.Id, cancellationToken)
            .TapAsync(async entity =>
            {
                entity.LastSyncedAt = Option.Some(DateTimeOffset.UtcNow);
                await syncPassRepositories.AccountRepository.UpsertAsync(entity, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);

        account.LastSyncedAt = Option.Some(DateTimeOffset.UtcNow);

        return SyncPassResultFactory.Create(didRun: true, failedJobCount: failedJobCount);
    }

    private async Task RunProducerAsync(OneDriveAccount account, AccountSyncConfig syncConfig, Func<CancellationToken, Task<string>> tokenFactory, Func<SyncConflict, Task> conflictCallback, Action<int>? enumerationProgress, Action<string>? stageChanged, RemoteEnumerationContext context, Action<SyncProgressEventArgs>? onProgress, ChannelWriter<SyncJob> writer, TaskCompletionSource<bool> firstJobSignal, IReadOnlyList<FileClassificationCategory> mappings, CancellationToken cancellationToken)
    {
        bool signaled = false;
        try
        {
            await foreach (var item in dependencies.RemoteFolderEnumerator.StreamAsync(account, tokenFactory, context, enumerationProgress, stageChanged, cancellationToken).ConfigureAwait(false))
            {
                var job = await dependencies.DownloadJobBuilder.BuildOneAsync(account, syncConfig, item, context.Rules, context.SyncedItems, conflictCallback, mappings, cancellationToken).ConfigureAwait(false);
                if (job is not null)
                {
                    await writer.WriteAsync(job, cancellationToken).ConfigureAwait(false);
                    if (!signaled)
                    {
                        firstJobSignal.TrySetResult(true);
                        signaled = true;
                    }
                }
            }

            if (context.HadNoRules)
                return;

            RaiseProgress(account.Id.Value, 0, 0, localizationService.GetLocal("Sync.DetectingRemoteDeletions"), onProgress);
            await dependencies.RemoteDeletionDetector.DetectAndApplyAsync(account.Id, context.SyncedItems, context.SeenRemoteIds, context.Rules, cancellationToken).ConfigureAwait(false);

            RaiseProgress(account.Id.Value, 0, 0, localizationService.GetLocal("Sync.DetectingLocalChanges"), onProgress);
            await dependencies.LocalDeletionDetector.DetectAndApplyAsync(account.Id, tokenFactory, context.SyncedItems, cancellationToken).ConfigureAwait(false);

            var syncedItemsByLocalPath = context.SyncedItems.Values.ToDictionary(i => i.LocalPath, StringComparer.OrdinalIgnoreCase);
            var uploadJobs = dependencies.LocalChangeDetector.DetectNewAndModifiedFiles(account.Id.Value, syncConfig.LocalSyncPath.Value, context.Rules, syncedItemsByLocalPath);

            foreach (var job in uploadJobs)
            {
                await writer.WriteAsync(job, cancellationToken).ConfigureAwait(false);
                if (!signaled)
                {
                    firstJobSignal.TrySetResult(true);
                    signaled = true;
                }
            }
        }
        catch (OperationCanceledException) when (!signaled)
        {
            firstJobSignal.TrySetCanceled(cancellationToken);
            throw;
        }
        catch (Exception ex) when (!signaled)
        {
            firstJobSignal.TrySetException(ex);
            throw;
        }
        finally
        {
            firstJobSignal.TrySetResult(false);
            writer.TryComplete();
        }
    }

    private static void RaiseProgress(string accountId, int completed, int total, string currentFile, Action<SyncProgressEventArgs>? onProgress)
        => onProgress?.Invoke(new SyncProgressEventArgs(accountId, string.Empty, completed, total, currentFile, SyncState.Syncing));
}
