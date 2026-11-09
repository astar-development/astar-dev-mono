using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeAccountActions : IAccountActions
{
    private readonly Lock gate = new();
    private readonly List<(string Action, AccountId AccountId)> calls = [];

    public Func<AccountId, Task<Result<Unit, AccountRemovalFailure>>> Remove { get; set; } = _ => Task.FromResult(Result.Success<Unit, AccountRemovalFailure>(Unit.Instance));

    public Func<AccountId, Task<Result<Unit, ReauthFailure>>> Reauthenticate { get; set; } = _ => Task.FromResult(Result.Success<Unit, ReauthFailure>(Unit.Instance));

    public IReadOnlyList<(string Action, AccountId AccountId)> Calls
    {
        get
        {
            lock (gate)
                return [.. calls];
        }
    }

    public Task<Result<Unit, AccountRemovalFailure>> RemoveAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        lock (gate)
            calls.Add(("remove", accountId));

        return Remove(accountId);
    }

    public Task<Result<Unit, ReauthFailure>> ReauthenticateAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        lock (gate)
            calls.Add(("sign-in", accountId));

        return Reauthenticate(accountId);
    }
}
