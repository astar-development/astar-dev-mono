using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FixedSyncRootReader : ISyncRootReader
{
    public Option<IDirectoryInfo> Root { get; set; } = Option.None<IDirectoryInfo>();

    public Result<Option<SyncRoot>, ErrorResponse>? Failure { get; set; }

    public Task<Result<Option<SyncRoot>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Failure ?? Result.Success<Option<SyncRoot>, ErrorResponse>(Root.Match(directory => Option.Some(SyncRootFactory.Create(directory).Match(root => root, _ => throw new InvalidOperationException("The test root must be valid."))), () => Option.None<SyncRoot>())));

    public Task<Result<IReadOnlyList<AccountSyncRoot>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
}
