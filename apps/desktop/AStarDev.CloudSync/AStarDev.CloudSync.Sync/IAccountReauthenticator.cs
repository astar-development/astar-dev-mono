using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Signs a stored account in again when its tokens expired (#1393). Providers register it keyed by <c>CloudProvider</c>; consumers get it through <see cref="IProviderCapability{TCapability}"/>.</summary>
public interface IAccountReauthenticator
{
    /// <summary>Runs an interactive sign-in for the stored account.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the sign-in.</param>
    Task<Result<Unit, ReauthFailure>> ReauthenticateAsync(AccountId accountId, CancellationToken cancellationToken);
}
