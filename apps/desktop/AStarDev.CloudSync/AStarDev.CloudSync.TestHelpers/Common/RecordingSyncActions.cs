using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Common;

public sealed class RecordingSyncActions : ISyncActions
{
    private readonly List<string> calls = [];

    public IReadOnlyList<string> Calls => calls;

    public Task SyncNowAsync(AccountId accountId, CancellationToken cancellationToken) => Record($"{nameof(SyncNowAsync)}:{accountId}");

    public Task CancelSyncAsync(AccountId accountId, CancellationToken cancellationToken) => Record($"{nameof(CancelSyncAsync)}:{accountId}");

    public Task PauseAsync(AccountId accountId, CancellationToken cancellationToken) => Record($"{nameof(PauseAsync)}:{accountId}");

    public Task AddAccountAsync(CancellationToken cancellationToken) => Record(nameof(AddAccountAsync));

    public Task AddSyncPairAsync(Option<AccountId> accountId, CancellationToken cancellationToken) => Record($"{nameof(AddSyncPairAsync)}:{accountId.Match(id => id.ToString(), () => "none")}");

    public Task KeepBothAsync(SyncConflict conflict, CancellationToken cancellationToken) => Record($"{nameof(KeepBothAsync)}:{conflict.SyncPairId}:{conflict.FileName}");

    public Task KeepLocalAsync(SyncConflict conflict, CancellationToken cancellationToken) => Record($"{nameof(KeepLocalAsync)}:{conflict.SyncPairId}:{conflict.FileName}");

    public Task KeepCloudAsync(SyncConflict conflict, CancellationToken cancellationToken) => Record($"{nameof(KeepCloudAsync)}:{conflict.SyncPairId}:{conflict.FileName}");

    private Task Record(string call)
    {
        calls.Add(call);

        return Task.CompletedTask;
    }
}
