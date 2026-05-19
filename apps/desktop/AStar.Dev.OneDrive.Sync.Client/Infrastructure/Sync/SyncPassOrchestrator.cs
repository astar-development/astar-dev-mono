using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

internal sealed class SyncPassOrchestrator(IAccountRepository accountRepository, IDriveStateRepository driveStateRepository, SyncServiceDependencies dependencies) : ISyncPassOrchestrator
{
    public async Task<bool> OrchestrateAsync(OneDriveAccount account, string token, Func<SyncConflict, Task> conflictCallback, Action<SyncProgressEventArgs>? onProgress = null, Action<JobCompletedEventArgs>? onJobCompleted = null, CancellationToken ct = default)
    {
        var driveState = await driveStateRepository.GetByAccountIdAsync(account.Id, ct)
                             .OrElseAsync(new DriveStateEntity { AccountId = account.Id }).ConfigureAwait(false);

        driveState.LastSyncStartedAt = DateTimeOffset.UtcNow;
        driveState.DeltaLink = null;
        await driveStateRepository.UpsertAsync(driveState, ct).ConfigureAwait(false);

        var enumerationResult = await dependencies.RemoteFolderEnumerator.EnumerateAsync(account, token, ct).ConfigureAwait(false);

        if(enumerationResult.HadNoRules)
            return false;

        var syncedItemsDict = new Dictionary<string, SyncedItemEntity>(enumerationResult.SyncedItems);

        RaiseProgress(account.Id.Id, 0, 0, "Detecting remote deletions...", onProgress);
        await dependencies.RemoteDeletionDetector.DetectAndApplyAsync(account.Id, syncedItemsDict, enumerationResult.SeenRemoteIds, enumerationResult.Rules, ct).ConfigureAwait(false);

        RaiseProgress(account.Id.Id, 0, 0, "Detecting local changes...", onProgress);
        await dependencies.LocalDeletionDetector.DetectAndApplyAsync(account.Id, token, syncedItemsDict, ct).ConfigureAwait(false);

        var downloadJobs = await dependencies.DownloadJobBuilder.BuildAsync(account, enumerationResult.DeltaItems, enumerationResult.Rules, syncedItemsDict, conflictCallback, ct).ConfigureAwait(false);

        var syncedItemsByLocalPath = syncedItemsDict.Values.ToDictionary(i => i.LocalPath, StringComparer.OrdinalIgnoreCase);
        var uploadJobs = dependencies.LocalChangeDetector.DetectNewAndModifiedFiles(account.Id.Id, account.SyncConfig!.LocalSyncPath.Value, enumerationResult.Rules, syncedItemsByLocalPath);

        var allJobs = new List<SyncJob>(downloadJobs.Count + uploadJobs.Count);
        allJobs.AddRange(downloadJobs);
        allJobs.AddRange(uploadJobs);

        if(allJobs.Count > 0)
        {
            RaiseProgress(account.Id.Id, 0, allJobs.Count, $"Syncing {allJobs.Count} file(s)...", onProgress);
            await dependencies.JobExecutor.ExecuteAsync(account, token, allJobs, syncedItemsDict, onProgress ?? (_ => { }), onJobCompleted ?? (_ => { }), ct).ConfigureAwait(false);
        }
        else
        {
            onProgress?.Invoke(new SyncProgressEventArgs(account.Id.Id, string.Empty, 0, 0, "No changes", SyncState.Idle));
        }

        await accountRepository.GetByIdAsync(account.Id, ct)
            .TapAsync(async entity =>
            {
                entity.LastSyncedAt = DateTimeOffset.UtcNow;
                await accountRepository.UpsertAsync(entity, ct).ConfigureAwait(false);
            }).ConfigureAwait(false);

        account.LastSyncedAt = DateTimeOffset.UtcNow;

        return true;
    }

    private static void RaiseProgress(string accountId, int completed, int total, string currentFile, Action<SyncProgressEventArgs>? onProgress)
        => onProgress?.Invoke(new SyncProgressEventArgs(accountId, string.Empty, completed, total, currentFile, SyncState.Syncing));
}
