using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeSyncRootReader : ISyncRootReader
{
    public IReadOnlyList<AccountSyncRoot> Roots { get; set; } = [];

    public bool Fails { get; set; }

    public Task<Result<Option<SyncRoot>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Fails
            ? Result.Failure<Option<SyncRoot>, ErrorResponse>(new ErrorResponse("read failed"))
            : Result.Success<Option<SyncRoot>, ErrorResponse>(Roots.Where(entry => entry.AccountId == accountId).Select(entry => entry.Root).FirstOrNone()));

    public Task<Result<IReadOnlyList<AccountSyncRoot>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Fails
            ? Result.Failure<IReadOnlyList<AccountSyncRoot>, ErrorResponse>(new ErrorResponse("read failed"))
            : Result.Success<IReadOnlyList<AccountSyncRoot>, ErrorResponse>(Roots));
}
