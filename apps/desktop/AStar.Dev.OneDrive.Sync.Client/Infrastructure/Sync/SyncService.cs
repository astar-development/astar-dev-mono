using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public sealed class SyncService(IAuthService authService, IAccountRepository accountRepository, IDriveStateRepository driveStateRepository, ISyncRepository syncRepository, IHttpDownloader httpDownloader, IGraphService graphService, SyncServiceDependencies dependencies) : ISyncService
{
    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
    public event EventHandler<JobCompletedEventArgs>?  JobCompleted;
    public event EventHandler<SyncConflict>?           ConflictDetected;

    public async Task SyncAccountAsync(OneDriveAccount account, CancellationToken ct = default)
    {
        Serilog.Log.Information("[SyncService] SyncAccountAsync for {Email}", account.Email);
        RaiseProgress(account.Id.Id, 0, 0, "Authenticating...", SyncState.Syncing);

        var authResult = await authService.AcquireTokenSilentAsync(account.Id.Id, ct);

        if(!authResult.IsSuccess)
        {
            RaiseProgress(account.Id.Id, 0, 0, authResult.ErrorMessage ?? "Auth failed", SyncState.Error);
            return;
        }

        if(account.LocalSyncPath is null)
        {
            RaiseProgress(account.Id.Id, 0, 0, "No local sync path configured", SyncState.Error);
            return;
        }

        try
        {
            await SyncAccountInternalAsync(account, authResult.AccessToken!, ct);
        }
        catch(OperationCanceledException)
        {
            RaiseProgress(account.Id.Id, 0, 0, "Sync cancelled", SyncState.Idle);
        }
        catch(Exception ex)
        {
            Serilog.Log.Error(ex, "[SyncService] Unhandled error syncing {Email}: {Error}", account.Email, ex.Message);
            RaiseProgress(account.Id.Id, 0, 0, ex.Message, SyncState.Error);
        }
    }

    public async Task ResolveConflictAsync(SyncConflict conflict, ConflictPolicy policy, CancellationToken ct = default)
    {
        var authResult = await authService.AcquireTokenSilentAsync(conflict.AccountId, ct);

        if(!authResult.IsSuccess)
            return;

        var outcome = ConflictResolver.Resolve(policy, conflict.LocalModified, conflict.RemoteModified);

        await ApplyConflictOutcomeAsync(conflict, outcome, authResult.AccessToken!, ct);
        await syncRepository.ResolveConflictAsync(conflict.Id, policy);
    }

    private async Task SyncAccountInternalAsync(OneDriveAccount account, string token, CancellationToken ct)
    {
        var driveState = await driveStateRepository.GetByAccountIdAsync(account.Id, ct)
                         ?? new DriveStateEntity { AccountId = account.Id };

        driveState.LastSyncStartedAt = DateTimeOffset.UtcNow;
        driveState.DeltaLink         = null;
        await driveStateRepository.UpsertAsync(driveState, ct);

        var enumerationResult = await dependencies.RemoteFolderEnumerator.EnumerateAsync(
            account,
            token,
            async conflict =>
            {
                await syncRepository.AddConflictAsync(conflict);
                ConflictDetected?.Invoke(this, conflict);
            },
            ct);

        if(enumerationResult.HadNoRules)
        {
            RaiseProgress(account.Id.Id, 0, 0, "No folders selected", SyncState.Idle);
            return;
        }

        RaiseProgress(account.Id.Id, 0, 0, "Detecting remote deletions...", SyncState.Syncing);
        await dependencies.RemoteDeletionDetector.DetectAndApplyAsync(account.Id, enumerationResult.SyncedItems, enumerationResult.SeenRemoteIds, enumerationResult.Rules, ct);

        RaiseProgress(account.Id.Id, 0, 0, "Detecting local changes...", SyncState.Syncing);
        await dependencies.LocalDeletionDetector.DetectAndApplyAsync(account.Id, token, enumerationResult.SyncedItems, ct);

        var localPathLookup = enumerationResult.SyncedItems.Values.ToDictionary(i => i.LocalPath, StringComparer.OrdinalIgnoreCase);
        var uploadJobs      = dependencies.LocalChangeDetector.DetectNewAndModifiedFiles(account.Id.Id, account.LocalSyncPath!.Value, enumerationResult.Rules, localPathLookup);

        var allJobs = new List<SyncJob>(enumerationResult.DownloadJobs.Count + uploadJobs.Count);
        allJobs.AddRange(enumerationResult.DownloadJobs);
        allJobs.AddRange(uploadJobs);

        if(allJobs.Count > 0)
        {
            RaiseProgress(account.Id.Id, 0, allJobs.Count, $"Syncing {allJobs.Count} file(s)...", SyncState.Syncing);
            await dependencies.JobExecutor.ExecuteAsync(
                account,
                token,
                allJobs,
                enumerationResult.SyncedItems,
                args => SyncProgressChanged?.Invoke(this, args),
                args => JobCompleted?.Invoke(this, args),
                ct);
        }
        else
        {
            RaiseProgress(account.Id.Id, 0, 0, "No changes", SyncState.Idle);
        }

        var accountEntity = await accountRepository.GetByIdAsync(account.Id, ct);
        if(accountEntity is not null)
        {
            accountEntity.LastSyncedAt = DateTimeOffset.UtcNow;
            await accountRepository.UpsertAsync(accountEntity, ct);
        }

        account.LastSyncedAt = DateTimeOffset.UtcNow;

        Serilog.Log.Information("[SyncService] Sync complete for {Email}", account.Email);
        RaiseProgress(account.Id.Id, 0, 0, "Sync complete", SyncState.Idle);
    }

    private async Task ApplyConflictOutcomeAsync(SyncConflict conflict, ConflictOutcome outcome, string accessToken, CancellationToken ct)
    {
        switch(outcome)
        {
            case ConflictOutcome.UseRemote:
                string downloadUrl = await graphService.GetDownloadUrlAsync(accessToken, conflict.RemoteItemId, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"No download URL could be resolved for conflict item '{conflict.RelativePath}' (itemId={conflict.RemoteItemId}).");

                await httpDownloader.DownloadAsync(downloadUrl, conflict.LocalPath, conflict.RemoteModified, ct: ct);
                break;

            case ConflictOutcome.KeepBoth:
                string keepBothName = ConflictResolver.MakeKeepBothName(conflict.LocalPath, conflict.LocalModified);
                if(File.Exists(conflict.LocalPath))
                    File.Move(conflict.LocalPath, keepBothName);
                break;
        }
    }

    private void RaiseProgress(string accountId, int completed, int total, string currentFile, SyncState syncState)
        => SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(accountId, string.Empty, completed, total, currentFile, syncState));
}
