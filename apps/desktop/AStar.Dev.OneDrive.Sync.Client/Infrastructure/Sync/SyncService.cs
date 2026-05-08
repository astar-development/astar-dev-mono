using System.IO.Abstractions;
using System.Reactive;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public sealed class SyncService(IAuthService authService, IAccountRepository accountRepository, IDriveStateRepository driveStateRepository, ISyncRepository syncRepository, IHttpDownloader httpDownloader, IGraphService graphService, SyncServiceDependencies dependencies, IFileSystem fileSystem) : ISyncService
{
    private readonly SyncPassOrchestrator syncPassOrchestrator = new(accountRepository, driveStateRepository, dependencies);

    /// <inheritdoc />
    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;

    /// <inheritdoc />
    public event EventHandler<JobCompletedEventArgs>? JobCompleted;

    /// <inheritdoc />
    public event EventHandler<SyncConflict>? ConflictDetected;

    /// <inheritdoc />
    public async Task SyncAccountAsync(OneDriveAccount account, CancellationToken ct = default)
    {
        Serilog.Log.Information("[SyncService] SyncAccountAsync for {Email}", account.Profile.Email);
        RaiseProgress(account.Id.Id, 0, 0, "Authenticating...", SyncState.Syncing);

        var authResult = await authService.AcquireTokenSilentAsync(account.Id.Id, ct).ConfigureAwait(false);

        if(authResult is not Result<AuthResult, AuthError>.Ok authOk)
        {
            var errorMessage = authResult is Result<AuthResult, AuthError>.Error { Reason: AuthFailedError failed }
                ? failed.Message
                : "Auth failed";
            RaiseProgress(account.Id.Id, 0, 0, errorMessage, SyncState.Error);

            return;
        }

        if(account.SyncConfig is null)
        {
            RaiseProgress(account.Id.Id, 0, 0, "No local sync path configured", SyncState.Error);

            return;
        }

        try
        {
            var didRun = await syncPassOrchestrator.OrchestrateAsync(
                account,
                authOk.Value.AccessToken,
                async conflict =>
                {
                    await syncRepository.AddConflictAsync(conflict).ConfigureAwait(false);
                    ConflictDetected?.Invoke(this, conflict);
                },
                args => SyncProgressChanged?.Invoke(this, args),
                args => JobCompleted?.Invoke(this, args),
                ct).ConfigureAwait(false);

            if(!didRun)
                RaiseProgress(account.Id.Id, 0, 0, "No folders selected", SyncState.Idle);
            else
            {
                Serilog.Log.Information("[SyncService] Sync complete for {Email}", account.Profile.Email);
                RaiseProgress(account.Id.Id, 0, 0, "Sync complete", SyncState.Idle);
            }
        }
        catch(OperationCanceledException)
        {
            RaiseProgress(account.Id.Id, 0, 0, "Sync cancelled", SyncState.Idle);
        }
        catch(Exception ex)
        {
            Serilog.Log.Error(ex, "[SyncService] Unhandled error syncing {Email}: {Error}", account.Profile.Email, ex.Message);
            RaiseProgress(account.Id.Id, 0, 0, ex.Message, SyncState.Error);
        }
    }

    /// <inheritdoc />
    public async Task ResolveConflictAsync(SyncConflict conflict, ConflictPolicy policy, CancellationToken ct = default)
    {
        var authResult = await authService.AcquireTokenSilentAsync(conflict.Remote.AccountId.Id, ct).ConfigureAwait(false);

        if(authResult is not Result<AuthResult, AuthError>.Ok authOk)
            return;

        var outcome = ConflictResolver.Resolve(policy, conflict.Snapshot.LocalModified, conflict.Snapshot.RemoteModified);

        await ApplyConflictOutcomeAsync(conflict, outcome, conflict.Remote.AccountId.Id, authOk.Value.AccessToken, ct).ConfigureAwait(false);
        await syncRepository.ResolveConflictAsync(conflict.Id, policy).ConfigureAwait(false);
    }

    private async Task ApplyConflictOutcomeAsync(SyncConflict conflict, ConflictOutcome outcome, string accountId, string accessToken, CancellationToken ct)
    {
        switch(outcome)
        {
            case ConflictOutcome.UseRemote:
                var urlResult = await graphService.GetDownloadUrlAsync(accessToken, conflict.Remote.RemoteItemId.Id, ct).ConfigureAwait(false);

                await urlResult.Match(
                    async url =>
                    {
                        var downloadResult = await httpDownloader.DownloadAsync(url, conflict.Target.LocalPath, conflict.Snapshot.RemoteModified, ct: ct).ConfigureAwait(false);

                        if(downloadResult is Result<Unit, string>.Error downloadError)
                        {
                            Serilog.Log.Error("[SyncService] Download failed resolving conflict for {Path}: {Error}", conflict.Target.RelativePath, downloadError.Reason);
                            RaiseProgress(accountId, 0, 0, downloadError.Reason, SyncState.Error);
                        }
                    },
                    error =>
                    {
                        Serilog.Log.Error("[SyncService] Could not resolve download URL for conflict item {Path}: {Error}", conflict.Target.RelativePath, error);
                        RaiseProgress(accountId, 0, 0, error, SyncState.Error);

                        return Task.CompletedTask;
                    });
                break;

            case ConflictOutcome.KeepBoth:
                string keepBothName = ConflictResolver.MakeKeepBothName(conflict.Target.LocalPath, conflict.Snapshot.LocalModified, fileSystem);
                if(fileSystem.File.Exists(conflict.Target.LocalPath))
                    fileSystem.File.Move(conflict.Target.LocalPath, keepBothName);
                break;
        }
    }

    private void RaiseProgress(string accountId, int completed, int total, string currentFile, SyncState syncState)
        => SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(accountId, string.Empty, completed, total, currentFile, syncState));
}
