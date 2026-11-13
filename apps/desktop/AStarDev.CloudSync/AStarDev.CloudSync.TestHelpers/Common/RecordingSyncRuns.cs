using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.TestHelpers.Common;

public sealed class RecordingSyncRuns : ISyncNowAction, ISyncCancelAction
{
    private readonly List<(string Action, AccountId AccountId, CancellationToken Token)> calls = [];

    public IReadOnlyList<(string Action, AccountId AccountId, CancellationToken Token)> Calls => calls;

    public Task SyncNowAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        calls.Add(("sync-now", accountId, cancellationToken));

        return Task.CompletedTask;
    }

    public Task CancelAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        calls.Add(("cancel", accountId, cancellationToken));

        return Task.CompletedTask;
    }
}
