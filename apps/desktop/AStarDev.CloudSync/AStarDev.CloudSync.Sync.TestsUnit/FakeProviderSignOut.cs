using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeProviderSignOut(List<string> steps) : IProviderSignOut
{
    public Result<Unit, ProviderError> Outcome { get; set; } = Result.Success<Unit, ProviderError>(Unit.Instance);

    public Task<Result<Unit, ProviderError>> SignOutAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        lock (steps)
            steps.Add("sign-out");

        return Task.FromResult(Outcome);
    }
}
