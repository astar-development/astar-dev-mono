using System.Threading.Channels;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Options;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

internal sealed class SyncPassOrchestrator(IAccountRepository accountRepository, IDriveStateRepository driveStateRepository, SyncServiceDependencies dependencies, IOptions<SyncSettings> syncSettings, ISettingsService settingsService, ILocalizationService localizationService) : ISyncPassOrchestrator
{
    public async Task<bool> OrchestrateAsync(OneDriveAccount account, AccountSyncConfig syncConfig, Func<CancellationToken, Task<string>> tokenFactory, Func<SyncConflict, Task> conflictCallback, Action<SyncProgressEventArgs>? onProgress = null, Func<JobCompletedEventArgs, Task>? onJobCompleted = null, CancellationToken ct = default)
    {
        var driveState = (await driveStateRepository.GetByAccountIdAsync(account.Id, ct).ConfigureAwait(false))
            .Match(v => v, () => new DriveStateEntity { AccountId = account.Id });

        driveState.LastSyncStartedAt = Option.Some(DateTimeOffset.UtcNow);
        driveState.DeltaLink = Option.None<string>();
        await driveStateRepository.UpsertAsync(driveState, ct).ConfigureAwait(false);

        int progressReportInterval = syncSettings.Value.ProgressReportInterval;
        int workerCount = settingsService.Current.ConcurrentWorkerCount;
        var context = new RemoteEnumerationContext();

        Action<int>? enumerationProgress = onProgress is null ? null : count =>
        {
            if (count % progressReportInterval == 0)
                RaiseProgress(account.Id.Id, count, 0, localizationService.GetLocal("Sync.Enumerating", count), onProgress);
        };

        var jobChannel = Channel.CreateBounded<SyncJob>(new BoundedChannelOptions(workerCount * 4) { FullMode = BoundedChannelFullMode.Wait, SingleReader = false, SingleWriter = true });
        var firstJobSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var producerTask = RunProducerAsync(account, syncConfig, tokenFactory, conflictCallback, enumerationProgress, context, onProgress, jobChannel.Writer, firstJobSignal, ct);

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

        if (hasJobs)
            await dependencies.JobExecutor.ExecuteAsync(account, tokenFactory, jobChannel.Reader.ReadAllAsync(ct), context.SyncedItems, onProgress ?? (_ => { }), onJobCompleted ?? (_ => Task.CompletedTask), ct).ConfigureAwait(false);

        await producerTask.ConfigureAwait(false);

        if (context.HadNoRules)
            return false;

        if (!hasJobs)
            onProgress?.Invoke(new SyncProgressEventArgs(account.Id.Id, string.Empty, 0, 0, localizationService.GetLocal("Sync.NoChanges"), SyncState.Idle));

        await accountRepository.GetByIdAsync(account.Id, ct)
            .TapAsync(async entity =>
            {
                entity.LastSyncedAt = Option.Some(DateTimeOffset.UtcNow);
                await accountRepository.UpsertAsync(entity, ct).ConfigureAwait(false);
            }).ConfigureAwait(false);

        account.LastSyncedAt = Option.Some(DateTimeOffset.UtcNow);

        return true;
    }

    private async Task RunProducerAsync(OneDriveAccount account, AccountSyncConfig syncConfig, Func<CancellationToken, Task<string>> tokenFactory, Func<SyncConflict, Task> conflictCallback, Action<int>? enumerationProgress, RemoteEnumerationContext context, Action<SyncProgressEventArgs>? onProgress, ChannelWriter<SyncJob> writer, TaskCompletionSource<bool> firstJobSignal, CancellationToken ct)
    {
        bool signaled = false;
        try
        {
            await foreach (var item in dependencies.RemoteFolderEnumerator.StreamAsync(account, tokenFactory, context, enumerationProgress, ct).ConfigureAwait(false))
            {
                var job = await dependencies.DownloadJobBuilder.BuildOneAsync(account, syncConfig, item, context.Rules, context.SyncedItems, conflictCallback, ct).ConfigureAwait(false);
                if (job is not null)
                {
                    await writer.WriteAsync(job, ct).ConfigureAwait(false);
                    if (!signaled)
                    {
                        firstJobSignal.TrySetResult(true);
                        signaled = true;
                    }
                }
            }

            if (context.HadNoRules)
                return;

            RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.DetectingRemoteDeletions"), onProgress);
            await dependencies.RemoteDeletionDetector.DetectAndApplyAsync(account.Id, context.SyncedItems, context.SeenRemoteIds, context.Rules, ct).ConfigureAwait(false);

            RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.DetectingLocalChanges"), onProgress);
            await dependencies.LocalDeletionDetector.DetectAndApplyAsync(account.Id, tokenFactory, context.SyncedItems, ct).ConfigureAwait(false);

            var syncedItemsByLocalPath = context.SyncedItems.Values.ToDictionary(i => i.LocalPath, StringComparer.OrdinalIgnoreCase);
            var uploadJobs = dependencies.LocalChangeDetector.DetectNewAndModifiedFiles(account.Id.Id, syncConfig.LocalSyncPath.Value, context.Rules, syncedItemsByLocalPath);

            foreach (var job in uploadJobs)
            {
                await writer.WriteAsync(job, ct).ConfigureAwait(false);
                if (!signaled)
                {
                    firstJobSignal.TrySetResult(true);
                    signaled = true;
                }
            }
        }
        catch (OperationCanceledException) when (!signaled)
        {
            firstJobSignal.TrySetCanceled(ct);
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
