using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeSyncProgressFeed : ISyncProgressFeed
{
    public event EventHandler<SyncProgressChangedEventArgs>? ProgressChanged;

    public Option<SyncProgress> ProgressOf(AccountId accountId) => Option.None<SyncProgress>();

    public void Raise(AccountId accountId, SyncProgress progress) => ProgressChanged?.Invoke(this, new SyncProgressChangedEventArgs(accountId, progress));
}
