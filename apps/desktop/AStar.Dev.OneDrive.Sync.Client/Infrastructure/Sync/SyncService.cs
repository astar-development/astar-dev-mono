using AStar.Dev.Functional.Extensions;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;

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
    public async Task SyncAccountAsync(OneDriveAccount account, CancellationToken cancellationToken = default)
    {
        OneDriveSyncClientMessages.SyncServiceStarting(logger, account.Id.Id);
        RaiseProgress(account.Id.Id, localizationService.GetLocal("Sync.Authenticating"), SyncState.Syncing);

        var initialAuth = await authService.AcquireTokenSilentAsync(account.Id.Id, cancellationToken).ConfigureAwait(false);
        bool authOk = initialAuth.Match(_ => true, _ => false);

        if (!authOk)
        {
            bool reAuthRequired = initialAuth.Match(_ => false, err => err is AuthReAuthRequiredError);
            RaiseProgress(account.Id.Id, GetSyncStatusText(reAuthRequired), GetSyncState(reAuthRequired));

            return;
        }

        _ = await RunSyncAsync(account, initialAuth, cancellationToken).ConfigureAwait(false);
    }

    private static SyncState GetSyncState(bool reAuthRequired) => reAuthRequired ? SyncState.ReAuthRequired : SyncState.Error;
    private string GetSyncStatusText(bool reAuthRequired) => localizationService.GetLocal(reAuthRequired ? "Sync.ReAuthRequired" : "Sync.AuthFailed");

    private async Task<bool> RunSyncAsync(OneDriveAccount account, Result<AuthResult, AuthError> initialAuth, CancellationToken cancellationToken)
    {
        if (account.SyncConfig is not Option<AccountSyncConfig>.Some syncConfigSome)
        {
            RaiseProgress(account.Id.Id, localizationService.GetLocal("Sync.NoSyncPath"), SyncState.Error);

            return false;
        }

        var syncConfig = syncConfigSome.Value;
        var (initialToken, initialExpiry) = initialAuth.Match(ok => (ok.AccessToken, ok.ExpiresOn), _ => (string.Empty, DateTimeOffset.MinValue));
        var tokenFactory = new CachedTokenFactory(account.Id.Id, authService, initialToken, initialExpiry);
        try
        {
            var syncResult = await syncPassOrchestrator.OrchestrateAsync(
                account,
                syncConfig,
                tokenFactory.GetTokenAsync,
                async conflict =>
                {
                    await syncRepository.AddConflictAsync(conflict, cancellationToken).ConfigureAwait(false);
                    ConflictDetected?.Invoke(this, conflict);
                },
                args => SyncProgressChanged?.Invoke(this, args),
                args => { JobCompleted?.Invoke(this, args); return Task.CompletedTask; },
                cancellationToken).ConfigureAwait(false);

            if (!syncResult.DidRun)
            {
                RaiseProgress(account.Id.Id, localizationService.GetLocal("Sync.NoFoldersSelected"), SyncState.Idle);
            }
            else if (syncResult.FailedJobCount > 0)
            {
                OneDriveSyncClientMessages.SyncServiceComplete(logger, account.Id.Id);
                RaiseProgress(account.Id.Id, localizationService.GetLocal("Sync.CompletedWithErrors", syncResult.FailedJobCount), SyncState.Error);
            }
            else
            {
                OneDriveSyncClientMessages.SyncServiceComplete(logger, account.Id.Id);
                RaiseProgress(account.Id.Id, localizationService.GetLocal("Sync.Complete"), SyncState.Idle);
            }
        }
        catch (OperationCanceledException)
        {
            RaiseProgress(account.Id.Id, localizationService.GetLocal("Sync.Cancelled"), SyncState.Idle);
        }
        catch (SyncReAuthRequiredException)
        {
            OneDriveSyncClientMessages.SyncServiceReAuthRequired(logger, account.Id.Id);
            RaiseProgress(account.Id.Id, localizationService.GetLocal("Sync.ReAuthRequired"), SyncState.ReAuthRequired);
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.SyncServiceError(logger, account.Id.Id, ex.Message, ex);
            RaiseProgress(account.Id.Id, localizationService.GetLocal("Sync.UnexpectedError"), SyncState.Error);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task ResolveConflictAsync(SyncConflict conflict, ConflictPolicy policy, CancellationToken cancellationToken = default)
    {
        var initialAuth = await authService.AcquireTokenSilentAsync(conflict.Remote.AccountId.Id, cancellationToken).ConfigureAwait(false);
        bool authOk = initialAuth.Match(_ => true, _ => false);

        if (!authOk)
            return;

        var (initialToken, initialExpiry) = initialAuth.Match(ok => (ok.AccessToken, ok.ExpiresOn), _ => (string.Empty, DateTimeOffset.MinValue));
        using var tokenFactory = new CachedTokenFactory(conflict.Remote.AccountId.Id, authService, initialToken, initialExpiry);

        var outcome = ConflictResolver.Resolve(policy, conflict.Snapshot.LocalModified, conflict.Snapshot.RemoteModified);
        bool applied = await conflictApplier.ApplyAsync(conflict, outcome, conflict.Remote.AccountId.Id, tokenFactory.GetTokenAsync, cancellationToken).ConfigureAwait(false);

        if (!applied)
        {
            RaiseProgress(conflict.Remote.AccountId.Id, localizationService.GetLocal("Sync.ConflictResolutionFailed"), SyncState.Error);

            return;
        }

        await syncRepository.ResolveConflictAsync(conflict.Id, policy, cancellationToken).ConfigureAwait(false);
        ConflictResolved?.Invoke(this, conflict);
    }

    private void RaiseProgress(string accountId, string currentFile, SyncState syncState)
        => SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(accountId, currentFile, syncState));
}
