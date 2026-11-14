using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ScheduleActivity : ISyncActivitySource
{
    private readonly HashSet<AccountId> running = [];

    public event EventHandler<SyncPassStartedEventArgs>? PassStarted
    {
        add { }
        remove { }
    }

    public event EventHandler<SyncPassFinishedEventArgs>? PassFinished
    {
        add { }
        remove { }
    }

    public void SetRunning(AccountId accountId) => running.Add(accountId);

    public AccountSyncActivity ActivityOf(AccountId accountId) => running.Contains(accountId) ? AccountSyncActivityFactory.CreateRunning(SyncTriggerFactory.CreateManual(), DateTimeOffset.UnixEpoch) : AccountSyncActivityFactory.CreateIdle();
}
