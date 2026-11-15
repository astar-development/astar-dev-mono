using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeAccountThrottleHints : IAccountThrottleHints
{
    private readonly Dictionary<AccountId, ThrottleHint> hints = [];

    public void Set(AccountId accountId, ThrottleHint hint)
    {
        hints[accountId] = hint;
    }

    public ThrottleHint HintFor(AccountId accountId) => hints.GetValueOrDefault(accountId, ThrottleHintFactory.CreateQuiet());
}
