using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>The actions the user can start from the main window. The single seam the view model commands call.</summary>
public interface ISyncActions
{
    /// <summary>Starts a sync of the account now.</summary>
    /// <param name="accountId">The account to sync.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    Task SyncNowAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Cancels the account's running sync.</summary>
    /// <param name="accountId">The account whose sync to cancel.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    Task CancelSyncAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Pauses syncing for the account.</summary>
    /// <param name="accountId">The account to pause.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    Task PauseAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Starts connecting a new cloud account, from any provider.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    Task AddAccountAsync(CancellationToken cancellationToken);

    /// <summary>Starts adding a sync pair (a local folder synced with a cloud folder).</summary>
    /// <param name="accountId">The account the pair belongs to, or none to let the user choose.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    Task AddSyncPairAsync(Option<AccountId> accountId, CancellationToken cancellationToken);
}
