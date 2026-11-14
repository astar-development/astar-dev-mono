using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeSyncRunStatusSource : ISyncRunStatusSource
{
    private readonly Dictionary<AccountId, SyncStatus> statuses = [];
    private readonly Dictionary<AccountId, DateTimeOffset> lastSyncs = [];

    public void Set(AccountId accountId, SyncStatus status, Option<DateTimeOffset> lastSync)
    {
        statuses[accountId] = status;
        _ = lastSync.Match(finished => lastSyncs[accountId] = finished, () => default);
    }

    public SyncStatus StatusOf(AccountId accountId) => statuses.GetValueOrDefault(accountId, SyncStatusFactory.CreateUpToDate());

    public Option<DateTimeOffset> LastSyncOf(AccountId accountId) => lastSyncs.TryGetValue(accountId, out var finished) ? Option.Some(finished) : Option.None<DateTimeOffset>();
}
