using AStarDev.CloudSync.Accounts;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="runner">Runs the sync.</param>
/// <param name="activity">Tells whether a run is in progress.</param>
/// <param name="cancel">Cancels the running run.</param>
/// <param name="dialog">Asks the user what to do when a run is in progress.</param>
/// <param name="logger">Logs the user's answer, without account details.</param>
internal sealed class SyncNowAction(ISyncPassRunner runner, ISyncActivitySource activity, ISyncCancelAction cancel, IConfirmSyncRestartDialog dialog, ILogger<SyncNowAction> logger) : ISyncNowAction
{
    private const string Location = nameof(SyncNowAction);

    /// <inheritdoc />
    public Task SyncNowAsync(AccountId accountId, CancellationToken cancellationToken) =>
        activity.ActivityOf(accountId) is AccountSyncActivity.Running ? AskThenRunAsync(accountId, cancellationToken) : RunAsync(accountId, cancellationToken);

    private async Task AskThenRunAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var choice = await dialog.AskAsync(accountId, cancellationToken).ConfigureAwait(false);
        if (choice is not SyncRestartChoice.CancelAndRestart)
        {
            LogMessage.Information(logger, Location, "Sync now was asked for while a run was in progress: the user kept the current run.");

            return;
        }

        LogMessage.Information(logger, Location, "Sync now was asked for while a run was in progress: the user chose to cancel it and start again.");
        await cancel.CancelAsync(accountId, cancellationToken).ConfigureAwait(false);
        await RunAsync(accountId, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunAsync(AccountId accountId, CancellationToken cancellationToken) =>
        _ = await Task.Run(() => runner.RunAsync(accountId, SyncTriggerFactory.CreateManual(), cancellationToken), cancellationToken).ConfigureAwait(false);
}
