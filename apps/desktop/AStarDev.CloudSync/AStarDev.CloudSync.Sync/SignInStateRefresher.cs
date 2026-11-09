using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Probes every stored account through its provider's <see cref="IAccountSignInProbe"/>.</summary>
/// <param name="accountStore">Lists the accounts.</param>
/// <param name="probes">Finds each account's probe.</param>
/// <param name="states">Receives the results.</param>
/// <param name="logger">Reports an unreadable account store.</param>
internal sealed class SignInStateRefresher(IAccountStore accountStore, IProviderCapability<IAccountSignInProbe> probes, IAccountSignInStates states, ILogger<SignInStateRefresher> logger) : ISignInStateRefresher
{
    private const string Location = nameof(SignInStateRefresher);

    /// <inheritdoc />
    public async Task RefreshAllAsync(CancellationToken cancellationToken)
    {
        var accounts = await accountStore.GetAllAsync(cancellationToken).MatchAsync(found => found, AccountsUnreadable).ConfigureAwait(false);
        await Task.WhenAll(accounts.Select(account => RefreshAsync(account.Id, cancellationToken))).ConfigureAwait(false);
    }

    private IReadOnlyList<StoredAccount> AccountsUnreadable(ErrorResponse error)
    {
        LogMessage.Warning(logger, Location, "The accounts could not be read for the sign-in check.");

        return [];
    }

    private async Task RefreshAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var state = await probes.ForAccountAsync(accountId, cancellationToken)
            .MatchAsync(probe => probe.ProbeAsync(accountId, cancellationToken), _ => Task.FromResult(Option.None<AccountSignInState>()))
            .ConfigureAwait(false);
        _ = state.Match(found => Remember(accountId, found), () => Unit.Instance);
    }

    private Unit Remember(AccountId accountId, AccountSignInState state)
    {
        states.Record(accountId, state);

        return Unit.Instance;
    }
}
