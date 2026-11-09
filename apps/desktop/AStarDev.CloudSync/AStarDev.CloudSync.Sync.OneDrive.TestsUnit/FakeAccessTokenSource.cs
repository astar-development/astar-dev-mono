using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.OneDrive;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class FakeAccessTokenSource : IAccessTokenSource
{
    private int calls;

    public AccessTokenOutcome Outcome { get; set; } = AccessTokenOutcomeFactory.CreateAcquired("secret-token-123");

    public int Calls => calls;

    public Task<AccessTokenOutcome> GetAccessTokenAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref calls);

        return Task.FromResult(Outcome);
    }
}
