using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Removes an account's tokens and per-account caches from the machine. Providers register it keyed by <c>CloudProvider</c>.</summary>
public interface IProviderSignOut
{
    /// <summary>Signs the account out. Signing out an account with no tokens succeeds.</summary>
    /// <param name="accountId">The stored account.</param>
    /// <param name="cancellationToken">Cancels the sign-out.</param>
    Task<Result<Unit, ProviderError>> SignOutAsync(AccountId accountId, CancellationToken cancellationToken);
}
