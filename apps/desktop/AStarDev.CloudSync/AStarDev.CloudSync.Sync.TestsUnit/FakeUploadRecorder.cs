using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeUploadRecorder(InMemorySyncBaseline store) : IUploadRecorder
{
    private readonly Lock gate = new();
    private readonly List<(SyncBaselineEntry Baseline, RemoteItemSnapshot Remote)> recorded = [];

    public bool Fails { get; set; }

    public IReadOnlyList<(SyncBaselineEntry Baseline, RemoteItemSnapshot Remote)> Recorded
    {
        get
        {
            lock (gate)
                return [.. recorded];
        }
    }

    public Task<Result<Unit, ErrorResponse>> RecordAsync(AccountId accountId, SyncBaselineEntry baseline, RemoteItemSnapshot remote, CancellationToken cancellationToken)
    {
        if (Fails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        lock (gate)
        {
            recorded.Add((baseline, remote));
            store.Add(baseline);
        }

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }
}
