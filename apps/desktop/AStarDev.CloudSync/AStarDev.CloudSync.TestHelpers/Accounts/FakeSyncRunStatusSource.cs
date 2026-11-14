using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeSyncRunStatusSource : ISyncRunStatusSource
{
    private readonly Dictionary<AccountId, SyncStatus> statuses = [];

    public void Set(AccountId accountId, SyncStatus status)
    {
        statuses[accountId] = status;
    }

    public SyncStatus StatusOf(AccountId accountId) => statuses.GetValueOrDefault(accountId, SyncStatusFactory.CreateUpToDate());
}
