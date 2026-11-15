using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Tray;

/// <inheritdoc />
/// <param name="accounts">Lists the accounts whose passes may be running.</param>
/// <param name="activity">Tells which accounts are syncing.</param>
/// <param name="actions">Cancels a pass.</param>
/// <param name="window">Brings the window forward so the question can be seen.</param>
/// <param name="confirm">Asks whether to quit while a sync is running.</param>
/// <param name="exit">Ends the process.</param>
/// <param name="logger">Reports what could not be cancelled, without account details.</param>
public sealed class AppShutdown(IAccountStore accounts, ISyncActivitySource activity, ISyncActions actions, IMainWindowVisibility window, IConfirmQuitDialog confirm, IApplicationExit exit, ILogger<AppShutdown> logger) : IAppShutdown
{
    private const string Location = nameof(AppShutdown);

    private int quitting;

    /// <inheritdoc />
    public ShutdownState State => Volatile.Read(ref quitting) == 1 ? ShutdownStateFactory.CreateQuitting() : ShutdownStateFactory.CreateRunning();

    /// <inheritdoc />
    public async Task RequestQuitAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref quitting, 1) == 1)
            return;

        _ = $"{window}{confirm}";

        _ = await Try.RunAsync(() => CancelRunningAsync(cancellationToken), cancellationToken).MatchAsync(done => done, _ => Warn("A running sync could not be cancelled before quitting.")).ConfigureAwait(false);
        exit.Shutdown();
    }

    private Task<Unit> CancelRunningAsync(CancellationToken cancellationToken) =>
        accounts.GetAllAsync(cancellationToken).MatchAsync(all => CancelAsync(all, cancellationToken), _ => Task.FromResult(Warn("The accounts could not be read, so no running sync was cancelled before quitting.")));

    private async Task<Unit> CancelAsync(IReadOnlyList<StoredAccount> all, CancellationToken cancellationToken)
    {
        foreach (var account in all.Where(account => activity.ActivityOf(account.Id) is AccountSyncActivity.Running))
            await actions.CancelSyncAsync(account.Id, cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Unit Warn(string message)
    {
        LogMessage.Warning(logger, Location, message);

        return Unit.Instance;
    }
}
