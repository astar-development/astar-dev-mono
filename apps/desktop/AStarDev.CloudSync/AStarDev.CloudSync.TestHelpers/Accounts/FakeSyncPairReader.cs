using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeSyncPairReader : ISyncPairReader
{
    public Func<AccountId, Result<IReadOnlyList<SyncPair>, ErrorResponse>> Pairs { get; set; } = _ => Result.Success<IReadOnlyList<SyncPair>, ErrorResponse>([]);

    public Task<Result<IReadOnlyList<SyncPair>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Pairs(accountId));
}
