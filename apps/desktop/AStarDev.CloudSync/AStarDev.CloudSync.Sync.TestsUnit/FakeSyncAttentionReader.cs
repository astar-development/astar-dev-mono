using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeSyncAttentionReader : ISyncAttentionReader
{
    public Result<IReadOnlyList<SyncAttentionItem>, ErrorResponse> Open { get; set; } = Result.Success<IReadOnlyList<SyncAttentionItem>, ErrorResponse>([]);

    public Task<Result<IReadOnlyList<SyncAttentionItem>, ErrorResponse>> GetOpenAsync(AccountId accountId, CancellationToken cancellationToken) => Task.FromResult(Open);

    public Task<Result<IReadOnlyList<RelativeSyncPath>, ErrorResponse>> GetConflictedPathsAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<AttentionCounts, ErrorResponse>> CountAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();
}
