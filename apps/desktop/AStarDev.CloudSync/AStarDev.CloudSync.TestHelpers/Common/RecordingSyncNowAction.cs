using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.TestHelpers.Common;

public sealed class RecordingSyncNowAction : ISyncNowAction, ISyncCancelAction
{
    public List<(AccountId AccountId, CancellationToken Token)> SyncNowCalls { get; } = [];

    public List<(AccountId AccountId, CancellationToken Token)> CancelCalls { get; } = [];

    public Task SyncNowAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        SyncNowCalls.Add((accountId, cancellationToken));

        return Task.CompletedTask;
    }

    public Task CancelAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        CancelCalls.Add((accountId, cancellationToken));

        return Task.CompletedTask;
    }
}
