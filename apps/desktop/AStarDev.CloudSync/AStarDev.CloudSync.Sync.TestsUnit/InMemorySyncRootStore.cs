using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class InMemorySyncRootStore(ISyncRootPolicy policy) : ISyncRootReader, ISyncRootWriter
{
    private readonly List<AccountSyncRoot> roots = [];

    public int LostRaces { get; set; }

    public int SetAttempts { get; private set; }

    public Option<Exception> ReadFails { get; init; } = Option.None<Exception>();

    public IReadOnlyList<AccountSyncRoot> Roots => roots;

    public Task<Result<Option<SyncRoot>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<Option<SyncRoot>, ErrorResponse>(roots.Where(entry => entry.AccountId == accountId).Select(entry => entry.Root).FirstOrNone()));

    public Task<Result<IReadOnlyList<AccountSyncRoot>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(ReadFails.Match(
            _ => Result.Failure<IReadOnlyList<AccountSyncRoot>, ErrorResponse>(new ErrorResponse("read failed")),
            () => Result.Success<IReadOnlyList<AccountSyncRoot>, ErrorResponse>([.. roots])));

    public Task<Result<SyncRoot, SyncRootRejection>> SetAsync(AccountId accountId, IDirectoryInfo candidate, CancellationToken cancellationToken)
    {
        SetAttempts++;

        if (LostRaces > 0)
        {
            LostRaces--;
            var other = AccountIdFactory.Create(Guid.NewGuid());
            roots.Add(AccountSyncRootFactory.Create(other, SyncRootFactory.Create(candidate).Match(root => root, _ => throw new InvalidOperationException("The test root must be valid."))));
        }

        var checkedRoot = policy.Check(accountId, candidate, roots);
        checkedRoot.Match(root => { roots.Add(AccountSyncRootFactory.Create(accountId, root)); return true; }, _ => false);

        return Task.FromResult(checkedRoot);
    }
}
