using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Opens the dialog where the user ticks which folders of an account to sync (#1397).</summary>
public interface IFolderSelectionDialog
{
    /// <summary>Shows the dialog and waits until the user applies or cancels.</summary>
    /// <param name="accountId">The account whose folders to choose, or none to let the user pick an account first.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    Task ShowAsync(Option<AccountId> accountId, CancellationToken cancellationToken);
}
