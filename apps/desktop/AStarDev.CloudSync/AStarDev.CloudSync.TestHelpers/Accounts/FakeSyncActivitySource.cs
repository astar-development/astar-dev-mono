using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeSyncActivitySource : ISyncActivitySource
{
    public event EventHandler<SyncPassStartedEventArgs>? PassStarted;

    public event EventHandler<SyncPassFinishedEventArgs>? PassFinished;

    public AccountSyncActivity ActivityOf(AccountId accountId) => AccountSyncActivityFactory.CreateIdle();

    public void RaiseStarted(AccountId accountId) => PassStarted?.Invoke(this, new SyncPassStartedEventArgs(accountId, SyncTriggerFactory.CreateManual(), DateTimeOffset.UnixEpoch));

    public void RaiseFinished(AccountId accountId) => PassFinished?.Invoke(this, new SyncPassFinishedEventArgs(accountId, SyncTriggerFactory.CreateManual(), Result.Failure<SyncPassSummary, SyncPassFailure>(SyncPassFailureFactory.CreateCancelled())));
}
