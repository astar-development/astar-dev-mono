namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Decides what closing the main window does (#1409): hide it to the tray and tell the user once, or, with no tray to come back from, minimise it.</summary>
/// <param name="window">Hides or minimises the window.</param>
/// <param name="tray">Says whether a tray icon can be seen.</param>
/// <param name="notice">The one-off "still running in the tray" notice.</param>
public sealed class WindowClosePolicy(IMainWindowVisibility window, ITrayAvailability tray, TrayCloseNotice notice)
{
    /// <summary>Handles a close request. It never throws.</summary>
    /// <param name="cancellationToken">Cancels the notice.</param>
    public Task CloseAsync(CancellationToken cancellationToken)
    {
        if (tray.Check() is TrayAvailability.Missing)
        {
            window.Minimise();

            return Task.CompletedTask;
        }

        window.Hide();

        return notice.ShowAsync(cancellationToken);
    }
}
