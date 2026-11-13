using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Asks the user what to do when Sync Now is pressed while a sync is already running for the account (SE-08, #1402).</summary>
public interface ISyncRestartPrompt
{
    /// <summary>Shows the question and waits for the answer.</summary>
    /// <param name="accountId">The account whose sync is running.</param>
    /// <param name="cancellationToken">Closes the question; the answer is then <see cref="SyncRestartChoice.KeepCurrent"/>.</param>
    Task<SyncRestartChoice> AskAsync(AccountId accountId, CancellationToken cancellationToken);
}
