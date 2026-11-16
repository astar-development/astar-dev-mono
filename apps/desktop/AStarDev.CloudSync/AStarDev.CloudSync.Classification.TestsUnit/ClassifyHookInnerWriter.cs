using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class ClassifyHookInnerWriter : ISyncBaselineWriter
{
    private int upserts;
    private int removes;

    internal Result<Unit, ErrorResponse> Outcome { get; set; } = Result.Success<Unit, ErrorResponse>(Unit.Instance);

    internal int Upserts => Volatile.Read(ref upserts);

    internal int Removes => Volatile.Read(ref removes);

    public Task<Result<Unit, ErrorResponse>> UpsertAsync(AccountId accountId, IReadOnlyList<SyncBaselineEntry> entries, CancellationToken cancellationToken)
    {
        _ = Interlocked.Increment(ref upserts);

        return Task.FromResult(Outcome);
    }

    public Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, IReadOnlyList<RelativeSyncPath> paths, CancellationToken cancellationToken)
    {
        _ = Interlocked.Increment(ref removes);

        return Task.FromResult(Outcome);
    }
}
