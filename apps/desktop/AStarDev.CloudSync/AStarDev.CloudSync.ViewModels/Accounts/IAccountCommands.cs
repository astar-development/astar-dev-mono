namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>The account actions the layouts offer (#1393, #1474).</summary>
public interface IAccountCommands
{
    /// <summary>Signs the account in again, showing progress and any failure.</summary>
    /// <param name="account">The account.</param>
    /// <param name="cancellationToken">Cancels the sign-in.</param>
    Task SignInAsync(AccountViewModel account, CancellationToken cancellationToken);

    /// <summary>Asks the user to confirm, then removes the account; local files are kept.</summary>
    /// <param name="account">The account.</param>
    /// <param name="cancellationToken">Cancels the wait for the dialog.</param>
    Task RemoveAsync(AccountViewModel account, CancellationToken cancellationToken);

    /// <summary>Shows the folder the account syncs into and lets the user change it (#1394).</summary>
    /// <param name="account">The account.</param>
    /// <param name="cancellationToken">Cancels the wait for the dialog.</param>
    Task ChangeSyncFolderAsync(AccountViewModel account, CancellationToken cancellationToken);
}
