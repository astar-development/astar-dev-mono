using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public sealed class SyncService(IAuthService authService, ISyncRepository syncRepository, ISyncPassOrchestrator syncPassOrchestrator, IConflictApplier conflictApplier, ILogger<SyncService> logger, ILocalizationService localizationService) : ISyncService
{
    /// <inheritdoc />
    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;

    /// <inheritdoc />
    public event EventHandler<JobCompletedEventArgs>? JobCompleted;

    /// <inheritdoc />
    public event EventHandler<SyncConflict>? ConflictDetected;

    /// <inheritdoc />
    public event EventHandler<SyncConflict>? ConflictResolved;

    /// <inheritdoc />
    public async Task SyncAccountAsync(OneDriveAccount account, CancellationToken ct = default)
    {
        OneDriveSyncClientMessages.SyncServiceStarting(logger, account.Profile.Email);
        RaiseProgress(account.Id.Id, 0, 0, "Authenticating...", SyncState.Syncing);

        string? accessToken = await authService.AcquireTokenSilentAsync(account.Id.Id, ct)
            .MatchAsync<AuthResult, AuthError, string?>(
                ok => ok.AccessToken,
                _ =>
                {
                    RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.AuthFailed"), SyncState.Error);
                    return null;
                }).ConfigureAwait(false);

        if (accessToken is null)
            return;

        if (account.SyncConfig is Option<AccountSyncConfig>.None)
        {
            RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.NoSyncPath"), SyncState.Error);

            return;
        }

        try
        {
            bool didRun = await syncPassOrchestrator.OrchestrateAsync(
                account,
                accessToken,
                async conflict =>
                {
                    await syncRepository.AddConflictAsync(conflict).ConfigureAwait(false);
                    ConflictDetected?.Invoke(this, conflict);
                },
                args => SyncProgressChanged?.Invoke(this, args),
                args => JobCompleted?.Invoke(this, args),
                ct).ConfigureAwait(false);

            if (!didRun)
                RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.NoFoldersSelected"), SyncState.Idle);
            else
            {
                OneDriveSyncClientMessages.SyncServiceComplete(logger, account.Profile.Email);
                RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.Complete"), SyncState.Idle);
            }
        }
        catch (OperationCanceledException)
        {
            RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.Cancelled"), SyncState.Idle);
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.SyncServiceError(logger, account.Profile.Email, ex.Message, ex);
            RaiseProgress(account.Id.Id, 0, 0, ex.Message, SyncState.Error);
        }
    }

    /// <inheritdoc />
    public async Task ResolveConflictAsync(SyncConflict conflict, ConflictPolicy policy, CancellationToken ct = default)
    {
        string? accessToken = await authService.AcquireTokenSilentAsync(conflict.Remote.AccountId.Id, ct)
            .MatchAsync<AuthResult, AuthError, string?>(ok => ok.AccessToken, _ => null).ConfigureAwait(false);

        if (accessToken is null)
            return;

        var outcome = ConflictResolver.Resolve(policy, conflict.Snapshot.LocalModified, conflict.Snapshot.RemoteModified);
        bool applied = await conflictApplier.ApplyAsync(conflict, outcome, conflict.Remote.AccountId.Id, accessToken, ct).ConfigureAwait(false);

        if (!applied)
        {
            RaiseProgress(conflict.Remote.AccountId.Id, 0, 0, localizationService.GetLocal("Sync.ConflictResolutionFailed"), SyncState.Error);

            return;
        }

        await syncRepository.ResolveConflictAsync(conflict.Id, policy).ConfigureAwait(false);
        ConflictResolved?.Invoke(this, conflict);
    }

    private void RaiseProgress(string accountId, int completed, int total, string currentFile, SyncState syncState)
        => SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(accountId, string.Empty, completed, total, currentFile, syncState));
}
