namespace AStarDev.CloudSync.Accounts;

/// <summary>Shows the add-account dialog and runs one add-account attempt in it.</summary>
public interface IAddAccountDialog
{
    /// <summary>Opens the dialog and starts signing in. Completes when the dialog closes. Calling it while the dialog is open waits for the open dialog instead of starting a second attempt.</summary>
    /// <param name="cancellationToken">Cancels the attempt and closes the dialog.</param>
    Task ShowAsync(CancellationToken cancellationToken);
}
