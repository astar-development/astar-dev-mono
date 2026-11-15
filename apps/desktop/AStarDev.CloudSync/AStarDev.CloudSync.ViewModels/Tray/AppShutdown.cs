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
        if (Volatile.Read(ref quitting) == 1)
            return;

        var choice = await Try.RunAsync(() => ConfirmAndCancelAsync(cancellationToken), cancellationToken).MatchAsync(chosen => chosen, _ => QuitDespite("A running sync could not be cancelled before quitting.")).ConfigureAwait(false);
        if (choice is QuitChoice.QuitAnyway)
            exit.Shutdown();
    }

    private Task<QuitChoice> ConfirmAndCancelAsync(CancellationToken cancellationToken) =>
        accounts.GetAllAsync(cancellationToken).MatchAsync(all => ConfirmAsync(all.Where(account => activity.ActivityOf(account.Id) is AccountSyncActivity.Running).ToList(), cancellationToken), _ => Task.FromResult(QuitDespite("The accounts could not be read, so no running sync was cancelled before quitting.")));

    private async Task<QuitChoice> ConfirmAsync(List<StoredAccount> running, CancellationToken cancellationToken)
    {
        if (running.Count > 0)
        {
            window.Show();
            if (await confirm.AskAsync(cancellationToken).ConfigureAwait(false) is QuitChoice.StayOpen)
                return QuitChoiceFactory.CreateStayOpen();
        }

        if (Interlocked.Exchange(ref quitting, 1) == 1)
            return QuitChoiceFactory.CreateStayOpen();

        foreach (var account in running)
            await actions.CancelSyncAsync(account.Id, cancellationToken).ConfigureAwait(false);

        return QuitChoiceFactory.CreateQuitAnyway();
    }

    private QuitChoice QuitDespite(string message)
    {
        LogMessage.Warning(logger, Location, message);
        _ = Interlocked.Exchange(ref quitting, 1);

        return QuitChoiceFactory.CreateQuitAnyway();
    }
}
