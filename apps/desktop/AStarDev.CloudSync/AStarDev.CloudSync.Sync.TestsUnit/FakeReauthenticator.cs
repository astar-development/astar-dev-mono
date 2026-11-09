using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeReauthenticator : IAccountReauthenticator
{
    public Func<CancellationToken, Task<Result<Unit, ReauthFailure>>> Reauthenticate { get; set; } = _ => Task.FromResult(Result.Success<Unit, ReauthFailure>(Unit.Instance));

    public Task<Result<Unit, ReauthFailure>> ReauthenticateAsync(AccountId accountId, CancellationToken cancellationToken) => Reauthenticate(cancellationToken);
}
