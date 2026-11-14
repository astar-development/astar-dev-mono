using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync;

/// <summary>Starts and cancels syncs through the sync run services, toggles pause, opens the add-account dialog for "add account" and the folder selection dialog for "add sync pair".</summary>
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
}
