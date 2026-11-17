using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts.TestsUnit;

internal sealed class OverlapRecordingDatabase : ICloudSyncDatabase
{
    private int running;
    private int mostAtOnce;
    private int calls;

    internal int MostAtOnce => Volatile.Read(ref mostAtOnce);

    internal int Calls => Volatile.Read(ref calls);

    internal bool ThrowOnFirstCall { get; init; }

    public async Task<Result<Unit, ErrorResponse>> MigrateAsync(CancellationToken cancellationToken)
    {
        var call = Interlocked.Increment(ref calls);
        var now = Interlocked.Increment(ref running);
        RecordPeak(now);
        await Task.Delay(5, cancellationToken);
        Interlocked.Decrement(ref running);

        if (ThrowOnFirstCall && call == 1)
            throw new InvalidOperationException("locked");

        return Result.Success<Unit, ErrorResponse>(Unit.Instance);
    }

    private void RecordPeak(int now)
    {
        var peak = Volatile.Read(ref mostAtOnce);
        while (now > peak && Interlocked.CompareExchange(ref mostAtOnce, now, peak) != peak)
            peak = Volatile.Read(ref mostAtOnce);
    }
}
