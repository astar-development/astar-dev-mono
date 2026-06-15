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
        OneDriveSyncClientMessages.SyncServiceStarting(logger, account.Id.Id);
        RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.Authenticating"), SyncState.Syncing);

        var initialAuth = await authService.AcquireTokenSilentAsync(account.Id.Id, ct).ConfigureAwait(false);
        bool authOk = initialAuth.Match(_ => true, _ => false);

        if (!authOk)
        {
            bool reAuthRequired = initialAuth.Match(_ => false, err => err is AuthReAuthRequiredError);
            RaiseProgress(account.Id.Id, 0, 0,
                localizationService.GetLocal(reAuthRequired ? "Sync.ReAuthRequired" : "Sync.AuthFailed"),
                reAuthRequired ? SyncState.ReAuthRequired : SyncState.Error);

            return;
        }

        if (account.SyncConfig is not Option<AccountSyncConfig>.Some syncConfigSome)
        {
            RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.NoSyncPath"), SyncState.Error);

            return;
        }

        var syncConfig = syncConfigSome.Value;
        var (initialToken, initialExpiry) = initialAuth.Match(ok => (ok.AccessToken, ok.ExpiresOn), _ => (string.Empty, DateTimeOffset.MinValue));
        using var tokenFactory = new CachedTokenFactory(account.Id.Id, authService, initialToken, initialExpiry);

        try
        {
            bool didRun = await syncPassOrchestrator.OrchestrateAsync(
                account,
                syncConfig,
                tokenFactory.GetTokenAsync,
                async conflict =>
                {
                    await syncRepository.AddConflictAsync(conflict, ct).ConfigureAwait(false);
                    ConflictDetected?.Invoke(this, conflict);
                },
                args => SyncProgressChanged?.Invoke(this, args),
                args => { JobCompleted?.Invoke(this, args); return Task.CompletedTask; },
                ct).ConfigureAwait(false);

            if (!didRun)
                RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.NoFoldersSelected"), SyncState.Idle);
            else
            {
                OneDriveSyncClientMessages.SyncServiceComplete(logger, account.Id.Id);
                RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.Complete"), SyncState.Idle);
            }
        }
        catch (OperationCanceledException)
        {
            RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.Cancelled"), SyncState.Idle);
        }
        catch (SyncReAuthRequiredException)
        {
            OneDriveSyncClientMessages.SyncServiceReAuthRequired(logger, account.Id.Id);
            RaiseProgress(account.Id.Id, 0, 0, localizationService.GetLocal("Sync.ReAuthRequired"), SyncState.ReAuthRequired);
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.SyncServiceError(logger, account.Id.Id, ex.Message, ex);
            RaiseProgress(account.Id.Id, 0, 0, ex.Message, SyncState.Error);
        }
    }

    /// <inheritdoc />
    public async Task ResolveConflictAsync(SyncConflict conflict, ConflictPolicy policy, CancellationToken ct = default)
    {
        var initialAuth = await authService.AcquireTokenSilentAsync(conflict.Remote.AccountId.Id, ct).ConfigureAwait(false);
        bool authOk = initialAuth.Match(_ => true, _ => false);

        if (!authOk)
            return;

        var (initialToken, initialExpiry) = initialAuth.Match(ok => (ok.AccessToken, ok.ExpiresOn), _ => (string.Empty, DateTimeOffset.MinValue));
        using var tokenFactory = new CachedTokenFactory(conflict.Remote.AccountId.Id, authService, initialToken, initialExpiry);

        var outcome = ConflictResolver.Resolve(policy, conflict.Snapshot.LocalModified, conflict.Snapshot.RemoteModified);
        bool applied = await conflictApplier.ApplyAsync(conflict, outcome, conflict.Remote.AccountId.Id, tokenFactory.GetTokenAsync, ct).ConfigureAwait(false);

        if (!applied)
        {
            RaiseProgress(conflict.Remote.AccountId.Id, 0, 0, localizationService.GetLocal("Sync.ConflictResolutionFailed"), SyncState.Error);

            return;
        }

        await syncRepository.ResolveConflictAsync(conflict.Id, policy, ct).ConfigureAwait(false);
        ConflictResolved?.Invoke(this, conflict);
    }

    private void RaiseProgress(string accountId, int completed, int total, string currentFile, SyncState syncState)
        => SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(accountId, string.Empty, completed, total, currentFile, syncState));
}
