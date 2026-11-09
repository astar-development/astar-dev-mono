using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeSignInProbe : IAccountSignInProbe
{
    public Func<AccountId, Option<AccountSignInState>> Probe { get; set; } = _ => Option.Some(AccountSignInStateFactory.CreateSignedIn());

    public Task<Option<AccountSignInState>> ProbeAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Probe(accountId));
}
