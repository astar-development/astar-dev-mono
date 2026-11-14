using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ScriptedDatabaseHealthCheck : IDatabaseHealthCheck
{
    private int calls;

    public int Calls => Volatile.Read(ref calls);

    public Result<Unit, DatabaseHealthFailure> Answer { get; set; } = Result.Success<Unit, DatabaseHealthFailure>(Unit.Instance);

    public Option<WriteGate> Gate { get; set; } = Option.None<WriteGate>();

    public bool Throws { get; set; }

    public Queue<Result<Unit, DatabaseHealthFailure>> Queued { get; } = new();

    public async Task<Result<Unit, DatabaseHealthFailure>> CheckAsync(CancellationToken cancellationToken)
    {
        _ = Interlocked.Increment(ref calls);
        if (Throws)
            throw new InvalidOperationException("the check blew up");

        await Gate.Match(held => held.PassAsync().WaitAsync(cancellationToken), () => Task.CompletedTask);

        lock (Queued)
            return Queued.TryDequeue(out var next) ? next : Answer;
    }
}
