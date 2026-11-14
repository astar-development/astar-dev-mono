using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync;

/// <summary>Starts and cancels syncs through the sync run services, toggles pause, opens the add-account dialog for "add account" and the folder selection dialog for "add sync pair". Conflict resolution is still a placeholder: it logs the request and does nothing else.</summary>
/// <param name="addAccountDialog">Shows the add-account dialog.</param>
/// <param name="folderSelectionDialog">Shows the folder selection dialog for "add sync pair".</param>
/// <param name="syncNow">Starts a manual sync.</param>
/// <param name="cancel">Cancels a running sync.</param>
/// <param name="pauseWriter">Pauses and resumes scheduled syncing.</param>
/// <param name="logger">Records each requested placeholder action and the pause outcome.</param>
public sealed class PlaceholderSyncActions(IAddAccountDialog addAccountDialog, IFolderSelectionDialog folderSelectionDialog, ISyncNowAction syncNow, ISyncCancelAction cancel, IAccountPauseWriter pauseWriter, ILogger<PlaceholderSyncActions> logger) : ISyncActions
{
    /// <inheritdoc />
    public Task SyncNowAsync(AccountId accountId, CancellationToken cancellationToken) => syncNow.SyncNowAsync(accountId, cancellationToken);

    /// <inheritdoc />
    public Task CancelSyncAsync(AccountId accountId, CancellationToken cancellationToken) => cancel.CancelAsync(accountId, cancellationToken);

    /// <inheritdoc />
    public Task PauseAsync(AccountId accountId, CancellationToken cancellationToken) =>
        pauseWriter.ToggleAsync(accountId, cancellationToken)
            .MatchAsync(LogPauseState, error => LogPauseFailure());

    /// <inheritdoc />
    public Task AddAccountAsync(CancellationToken cancellationToken) => addAccountDialog.ShowAsync(cancellationToken);

    /// <inheritdoc />
    public Task AddSyncPairAsync(Option<AccountId> accountId, CancellationToken cancellationToken) => folderSelectionDialog.ShowAsync(accountId, cancellationToken);

    /// <inheritdoc />
    public Task KeepBothAsync(SyncConflict conflict, CancellationToken cancellationToken) => LogRequestAsync("Keep both requested (placeholder: the sync engine is not implemented yet)", conflict.SyncPairId.ToString());

    /// <inheritdoc />
    public Task KeepLocalAsync(SyncConflict conflict, CancellationToken cancellationToken) => LogRequestAsync("Keep local requested (placeholder: the sync engine is not implemented yet)", conflict.SyncPairId.ToString());

    /// <inheritdoc />
    public Task KeepCloudAsync(SyncConflict conflict, CancellationToken cancellationToken) => LogRequestAsync("Keep cloud requested (placeholder: the sync engine is not implemented yet)", conflict.SyncPairId.ToString());

    private Unit LogPauseState(PauseState state)
    {
        LogMessage.Information(logger, nameof(PlaceholderSyncActions), state is PauseState.Paused ? "Scheduled syncing was paused for an account." : "Scheduled syncing was resumed for an account.");

        return Unit.Instance;
    }

    private Unit LogPauseFailure()
    {
        LogMessage.Warning(logger, nameof(PlaceholderSyncActions), "Pausing or resuming an account could not be saved.");

        return Unit.Instance;
    }

    private Task LogRequestAsync(string action, string target)
    {
        LogMessage.Information(logger, nameof(PlaceholderSyncActions), action, target);

        return Task.CompletedTask;
    }
}
