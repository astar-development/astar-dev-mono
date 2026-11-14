using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Asks the user what to do when Sync Now is pressed while the account is already syncing (SE-08, #1402).</summary>
public interface IConfirmSyncRestartDialog
{
    /// <summary>Shows the question and waits for the answer. Closing the dialog without choosing counts as keeping the current sync. Asking again while the question is open waits for the open question.</summary>
    /// <param name="accountId">The account that is syncing.</param>
    /// <param name="cancellationToken">Closes the dialog without an answer.</param>
    /// <returns>The user's choice.</returns>
    Task<SyncRestartChoice> AskAsync(AccountId accountId, CancellationToken cancellationToken);
}
