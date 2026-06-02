using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Options;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

internal sealed class SyncPassOrchestrator(IAccountRepository accountRepository, IDriveStateRepository driveStateRepository, SyncServiceDependencies dependencies, IOptions<SyncSettings> syncSettings) : ISyncPassOrchestrator
{
    public async Task<bool> OrchestrateAsync(OneDriveAccount account, Func<CancellationToken, Task<string>> tokenFactory, Func<SyncConflict, Task> conflictCallback, Action<SyncProgressEventArgs>? onProgress = null, Action<JobCompletedEventArgs>? onJobCompleted = null, CancellationToken ct = default)
    {
        var driveState = (await driveStateRepository.GetByAccountIdAsync(account.Id, ct).ConfigureAwait(false))
            .Match(v => v, () => new DriveStateEntity { AccountId = account.Id });

        driveState.LastSyncStartedAt = Option.Some(DateTimeOffset.UtcNow);
        driveState.DeltaLink = Option.None<string>();
        await driveStateRepository.UpsertAsync(driveState, ct).ConfigureAwait(false);

        int progressReportInterval = syncSettings.Value.ProgressReportInterval;
        Action<int>? enumerationProgress = onProgress is null ? null : count =>
        {
            if (count % progressReportInterval == 0)
                RaiseProgress(account.Id.Id, count, 0, $"Enumerating: {count:N0} item(s) found", onProgress);
        };

        var enumerationResult = await dependencies.RemoteFolderEnumerator.EnumerateAsync(account, tokenFactory, enumerationProgress, ct).ConfigureAwait(false);

        if (enumerationResult.HadNoRules)
            return false;

        var syncedItemsDict = new Dictionary<string, SyncedItemEntity>(enumerationResult.SyncedItems);

        RaiseProgress(account.Id.Id, 0, 0, "Detecting remote deletions...", onProgress);
        await dependencies.RemoteDeletionDetector.DetectAndApplyAsync(account.Id, syncedItemsDict, enumerationResult.SeenRemoteIds, enumerationResult.Rules, ct).ConfigureAwait(false);

        RaiseProgress(account.Id.Id, 0, 0, "Detecting local changes...", onProgress);
        await dependencies.LocalDeletionDetector.DetectAndApplyAsync(account.Id, tokenFactory, syncedItemsDict, ct).ConfigureAwait(false);

        var downloadJobs = await dependencies.DownloadJobBuilder.BuildAsync(account, enumerationResult.DeltaItems, enumerationResult.Rules, syncedItemsDict, conflictCallback, ct).ConfigureAwait(false);

        var syncedItemsByLocalPath = syncedItemsDict.Values.ToDictionary(i => i.LocalPath, StringComparer.OrdinalIgnoreCase);
        var uploadJobs = dependencies.LocalChangeDetector.DetectNewAndModifiedFiles(account.Id.Id, account.SyncConfig.Match(v => v, () => throw new InvalidOperationException("SyncConfig is None")).LocalSyncPath.Value, enumerationResult.Rules, syncedItemsByLocalPath);

        var allJobs = new List<SyncJob>(downloadJobs.Count + uploadJobs.Count);
        allJobs.AddRange(downloadJobs);
        allJobs.AddRange(uploadJobs);

        if (allJobs.Count > 0)
        {
            RaiseProgress(account.Id.Id, 0, allJobs.Count, $"Syncing {allJobs.Count:N0} file(s)...", onProgress);
            await dependencies.JobExecutor.ExecuteAsync(account, tokenFactory, allJobs, syncedItemsDict, onProgress ?? (_ => { }), onJobCompleted ?? (_ => { }), ct).ConfigureAwait(false);
        }
        else
        {
            onProgress?.Invoke(new SyncProgressEventArgs(account.Id.Id, string.Empty, 0, 0, "No changes", SyncState.Idle));
        }

        await accountRepository.GetByIdAsync(account.Id, ct)
            .TapAsync(async entity =>
            {
                entity.LastSyncedAt = Option.Some(DateTimeOffset.UtcNow);
                await accountRepository.UpsertAsync(entity, ct).ConfigureAwait(false);
            }).ConfigureAwait(false);

        account.LastSyncedAt = Option.Some(DateTimeOffset.UtcNow);

        return true;
    }

    private static void RaiseProgress(string accountId, int completed, int total, string currentFile, Action<SyncProgressEventArgs>? onProgress)
        => onProgress?.Invoke(new SyncProgressEventArgs(accountId, string.Empty, completed, total, currentFile, SyncState.Syncing));
}
