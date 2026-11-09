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
}
