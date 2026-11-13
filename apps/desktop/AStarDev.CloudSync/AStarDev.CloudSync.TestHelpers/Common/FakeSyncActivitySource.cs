using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Common;

public sealed class FakeSyncActivitySource : ISyncActivitySource
{
    private readonly Dictionary<AccountId, AccountSyncActivity> running = [];

    public event EventHandler<SyncPassStartedEventArgs>? PassStarted;

    public event EventHandler<SyncPassFinishedEventArgs>? PassFinished;

    public AccountSyncActivity ActivityOf(AccountId accountId) => running.GetValueOrDefault(accountId, AccountSyncActivityFactory.CreateIdle());

    public void Start(AccountId accountId, SyncTrigger trigger, DateTimeOffset startedAt)
    {
        running[accountId] = AccountSyncActivityFactory.CreateRunning(trigger, startedAt);
        PassStarted?.Invoke(this, new SyncPassStartedEventArgs(accountId, trigger, startedAt));
    }

    public void Finish(AccountId accountId, SyncTrigger trigger, Result<SyncPassSummary, SyncPassFailure> outcome)
    {
        _ = running.Remove(accountId);
        PassFinished?.Invoke(this, new SyncPassFinishedEventArgs(accountId, trigger, outcome));
    }
}
