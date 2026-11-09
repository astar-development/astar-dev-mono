using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Asks the provider, without user interaction, whether an account can still get tokens. Providers register it keyed by <c>CloudProvider</c>.</summary>
public interface IAccountSignInProbe
{
    /// <summary>Probes the account.</summary>
    /// <param name="accountId">The stored account.</param>
    /// <param name="cancellationToken">Cancels the probe.</param>
    /// <returns>The state, or none when the probe could not tell (network, keyring); an unknown result never marks the account as needing sign-in.</returns>
    Task<Option<AccountSignInState>> ProbeAsync(AccountId accountId, CancellationToken cancellationToken);
}
