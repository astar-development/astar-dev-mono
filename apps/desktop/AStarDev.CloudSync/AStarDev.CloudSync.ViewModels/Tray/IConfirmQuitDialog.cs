namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Asks the user whether to quit while a sync is running (#1409).</summary>
public interface IConfirmQuitDialog
{
    /// <summary>Shows the question and waits for the answer. Closing the dialog without choosing counts as staying open. Asking again while the question is open waits for the open question.</summary>
    /// <param name="cancellationToken">Closes the dialog without an answer.</param>
    /// <returns>The user's choice.</returns>
    Task<QuitChoice> AskAsync(CancellationToken cancellationToken);
}
