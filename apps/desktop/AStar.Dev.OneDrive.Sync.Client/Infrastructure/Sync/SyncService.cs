using AStar.Dev.FunctionalParadigm;
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

        var outcome = await authService.AcquireTokenSilentAsync(account.Id.Id, cancellationToken).MatchAsync(
            authResult => RunSyncPassAsync(account, authResult, cancellationToken),
            authError => Task.FromResult(SyncOutcomeFactory.CreateAuthFailed(authError is AuthReAuthRequiredError)));

        ApplyOutcome(account.Id.Id, outcome);
    }

    /// <inheritdoc />
    public async Task ResolveConflictAsync(SyncConflict conflict, ConflictPolicy policy, CancellationToken cancellationToken = default)
    {
        string accountId = conflict.Remote.AccountId.Id;

        bool applied = await authService.AcquireTokenSilentAsync(accountId, cancellationToken).MatchAsync(
            authResult => ApplyConflictAsync(conflict, policy, authResult, cancellationToken),
            _ => Task.FromResult(false));

        if (!applied)
        {
            RaiseProgress(accountId, localizationService.GetLocal("Sync.ConflictResolutionFailed"), SyncState.Error);

            return;
        }

        await syncRepository.ResolveConflictAsync(conflict.Id, policy, cancellationToken).ConfigureAwait(false);
        ConflictResolved?.Invoke(this, conflict);
    }

    private async Task<bool> ApplyConflictAsync(SyncConflict conflict, ConflictPolicy policy, AuthResult authResult, CancellationToken cancellationToken)
    {
        using var tokenFactory = new CachedTokenFactory(conflict.Remote.AccountId.Id, authService, authResult.AccessToken, authResult.ExpiresOn);
        var conflictOutcome = ConflictResolver.Resolve(policy, conflict.Snapshot.LocalModified, conflict.Snapshot.RemoteModified);

        return await conflictApplier.ApplyAsync(conflict, conflictOutcome, conflict.Remote.AccountId.Id, tokenFactory.GetTokenAsync, cancellationToken).ConfigureAwait(false);
    }

    private Task<SyncOutcome> RunSyncPassAsync(OneDriveAccount account, AuthResult authResult, CancellationToken cancellationToken)
        => account.SyncConfig.MatchAsync(
            syncConfig => ExecuteSyncPassAsync(account, syncConfig, authResult, cancellationToken),
            SyncOutcomeFactory.CreateNoSyncPath);

    private async Task<SyncOutcome> ExecuteSyncPassAsync(OneDriveAccount account, AccountSyncConfig syncConfig, AuthResult authResult, CancellationToken cancellationToken)
    {
        var tokenFactory = new CachedTokenFactory(account.Id.Id, authService, authResult.AccessToken, authResult.ExpiresOn);

        try
        {
            var exceptional = await Try.RunAsync(() => syncPassOrchestrator.OrchestrateAsync(
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
                cancellationToken)).ConfigureAwait(false);

            return exceptional.Match(
                DetermineOutcome,
                exception => exception is SyncReAuthRequiredException ? SyncOutcomeFactory.CreateReAuthRequired() : SyncOutcomeFactory.CreateUnexpectedError(exception));
        }
        catch (OperationCanceledException)
        {
            return SyncOutcomeFactory.CreateCancelled();
        }
    }

    private static SyncOutcome DetermineOutcome(SyncPassResult result) => result switch
    {
        { DidRun: false } => SyncOutcomeFactory.CreateNoFoldersSelected(),
        { FailedJobCount: > 0 } => SyncOutcomeFactory.CreateCompletedWithErrors(result.FailedJobCount),
        _ => SyncOutcomeFactory.CreateCompleted()
    };

    private void ApplyOutcome(string accountId, SyncOutcome outcome)
    {
        switch (outcome)
        {
            case SyncOutcome.NoSyncPath:
                RaiseProgress(accountId, localizationService.GetLocal("Sync.NoSyncPath"), SyncState.Error);
                break;

            case SyncOutcome.AuthFailed(var requiresReAuth):
                RaiseProgress(accountId, localizationService.GetLocal(requiresReAuth ? "Sync.ReAuthRequired" : "Sync.AuthFailed"), requiresReAuth ? SyncState.ReAuthRequired : SyncState.Error);
                break;

            case SyncOutcome.ReAuthRequired:
                OneDriveSyncClientMessages.SyncServiceReAuthRequired(logger, accountId);
                RaiseProgress(accountId, localizationService.GetLocal("Sync.ReAuthRequired"), SyncState.ReAuthRequired);
                break;

            case SyncOutcome.NoFoldersSelected:
                RaiseProgress(accountId, localizationService.GetLocal("Sync.NoFoldersSelected"), SyncState.Idle);
                break;

            case SyncOutcome.CompletedWithErrors(var failedJobCount):
                OneDriveSyncClientMessages.SyncServiceComplete(logger, accountId);
                RaiseProgress(accountId, localizationService.GetLocal("Sync.CompletedWithErrors", failedJobCount), SyncState.Error);
                break;

            case SyncOutcome.Completed:
                OneDriveSyncClientMessages.SyncServiceComplete(logger, accountId);
                RaiseProgress(accountId, localizationService.GetLocal("Sync.Complete"), SyncState.Idle);
                break;

            case SyncOutcome.Cancelled:
                RaiseProgress(accountId, localizationService.GetLocal("Sync.Cancelled"), SyncState.Idle);
                break;

            case SyncOutcome.UnexpectedError(var cause):
                OneDriveSyncClientMessages.SyncServiceError(logger, accountId, cause.Message, cause);
                RaiseProgress(accountId, localizationService.GetLocal("Sync.UnexpectedError"), SyncState.Error);
                break;
        }
    }

    private void RaiseProgress(string accountId, string currentFile, SyncState syncState)
        => SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(accountId, currentFile, syncState));
}
