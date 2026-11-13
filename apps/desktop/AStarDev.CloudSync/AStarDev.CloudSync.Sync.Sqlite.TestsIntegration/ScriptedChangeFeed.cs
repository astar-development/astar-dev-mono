using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class ScriptedChangeFeed : IRemoteChangeFeed
{
    private readonly Lock gate = new();
    private readonly Queue<(Result<RemoteChangePage, ProviderError> Result, Action? AfterRead)> script = new();
    private readonly List<string> requested = [];
    private int running;
    private int mostRunning;

    public IReadOnlyList<string> Requested
    {
        get
        {
            lock (gate)
                return [.. requested];
        }
    }

    public int MostRunningAtOnce => Volatile.Read(ref mostRunning);

    public void Enqueue(Result<RemoteChangePage, ProviderError> result, Action? afterRead = null)
    {
        lock (gate)
            script.Enqueue((result, afterRead));
    }

    public void Enqueue(RemoteChangePage page, Action? afterRead = null) => Enqueue(Result.Success<RemoteChangePage, ProviderError>(page), afterRead);

    public void Enqueue(ProviderError error) => Enqueue(Result.Failure<RemoteChangePage, ProviderError>(error));

    public async Task<Result<RemoteChangePage, ProviderError>> GetChangesAsync(AccountId accountId, Option<ChangeCursor> cursor, CancellationToken cancellationToken)
    {
        var now = Interlocked.Increment(ref running);
        InterlockedMax(now);
        await Task.Delay(10, cancellationToken);

        (Result<RemoteChangePage, ProviderError> Result, Action? AfterRead) next;
        lock (gate)
        {
            requested.Add(cursor.Match(found => found.Value, () => "none"));
            next = script.Count > 0 ? script.Dequeue() : (Result.Failure<RemoteChangePage, ProviderError>(ProviderErrorFactory.CreateUnexpected()), null);
        }

        _ = Interlocked.Decrement(ref running);
        next.AfterRead?.Invoke();

        return next.Result;
    }

    private void InterlockedMax(int value)
    {
        int current;

        do
        {
            current = Volatile.Read(ref mostRunning);

            if (value <= current)
                return;
        }
        while (Interlocked.CompareExchange(ref mostRunning, value, current) != current);
    }
}
