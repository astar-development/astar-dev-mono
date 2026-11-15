using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class RecordingActivityLogWriter : IActivityLogWriter
{
    private readonly Lock gate = new();
    private readonly List<ActivityEntry> appended = [];
    private int calls;

    public bool Fails { get; set; }

    public bool Throws { get; set; }

    public int Calls => Volatile.Read(ref calls);

    public IReadOnlyList<ActivityEntry> Appended
    {
        get
        {
            lock (gate)
            {
                return [.. appended];
            }
        }
    }

    public Task<Result<Unit, ErrorResponse>> AppendAsync(IReadOnlyList<ActivityEntry> entries, CancellationToken cancellationToken)
    {
        _ = Interlocked.Increment(ref calls);
        if (Throws)
            throw new InvalidOperationException("boom");

        if (Fails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        lock (gate)
        {
            appended.AddRange(entries);
        }

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }
}
