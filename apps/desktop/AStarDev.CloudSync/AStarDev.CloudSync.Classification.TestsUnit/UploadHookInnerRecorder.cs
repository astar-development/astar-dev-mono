using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class UploadHookInnerRecorder : IUploadRecorder
{
    private int calls;

    internal Result<Unit, ErrorResponse> Outcome { get; set; } = Result.Success<Unit, ErrorResponse>(Unit.Instance);

    internal int Calls => Volatile.Read(ref calls);

    public Task<Result<Unit, ErrorResponse>> RecordAsync(AccountId accountId, SyncBaselineEntry baseline, RemoteItemSnapshot remote, CancellationToken cancellationToken)
    {
        _ = Interlocked.Increment(ref calls);

        return Task.FromResult(Outcome);
    }
}
