using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class RecordingActivityLogWriter : IActivityLogWriter
{
    private readonly List<ActivityEntry> appended = [];

    public bool Fails { get; set; }

    public int Calls { get; private set; }

    public IReadOnlyList<ActivityEntry> Appended => appended;

    public Task<Result<Unit, ErrorResponse>> AppendAsync(IReadOnlyList<ActivityEntry> entries, CancellationToken cancellationToken)
    {
        Calls++;
        if (Fails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        appended.AddRange(entries);

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }
}
