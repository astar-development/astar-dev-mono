using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Probes every stored account through its provider's <see cref="IAccountSignInProbe"/>.</summary>
/// <param name="accountStore">Lists the accounts.</param>
/// <param name="probes">Finds each account's probe.</param>
/// <param name="states">Receives the results.</param>
internal sealed class SignInStateRefresher(IAccountStore accountStore, IProviderCapability<IAccountSignInProbe> probes, IAccountSignInStates states) : ISignInStateRefresher
{
    /// <inheritdoc />
    public async Task RefreshAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException($"{accountStore}{probes}{states}{cancellationToken}");
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
