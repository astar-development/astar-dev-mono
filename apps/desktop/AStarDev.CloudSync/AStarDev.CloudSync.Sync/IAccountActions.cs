using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>The account commands the UI offers (#1393). Kept apart from <c>ISyncActions</c>.</summary>
public interface IAccountActions
{
    /// <summary>Removes the account; local files are kept.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the removal before anything is deleted.</param>
    Task<Result<Unit, AccountRemovalFailure>> RemoveAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Signs the same account in again interactively, holding the account's lease so no pass or removal runs meanwhile. Success marks it <see cref="AccountSignInState.SignedIn"/>.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the sign-in.</param>
    Task<Result<Unit, ReauthFailure>> ReauthenticateAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Changes the folder the account syncs into, holding the account's lease so no pass runs meanwhile. A missing folder is created when its parent exists and can be written to; a folder created here is removed again if the change is rejected. Files in the old folder are left where they are.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="candidate">The new root.</param>
    /// <param name="cancellationToken">Cancels the change.</param>
    /// <returns>The saved root, or why it was rejected (<see cref="SyncRootRejection.SyncStillRunning"/> when the account stayed busy).</returns>
    Task<Result<SyncRoot, SyncRootRejection>> ChangeSyncRootAsync(AccountId accountId, IDirectoryInfo candidate, CancellationToken cancellationToken);
}
