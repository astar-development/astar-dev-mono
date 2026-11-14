namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncProgress"/> values.</summary>
public static class SyncProgressFactory
{
    /// <summary>Creates progress. Negative values become zero, and done never exceeds a known total.</summary>
    /// <param name="phase">The phase the run is in.</param>
    /// <param name="bytesDone">Bytes moved so far.</param>
    /// <param name="bytesTotal">The bytes the phase has to move.</param>
    public static SyncProgress Create(SyncPhase phase, long bytesDone, long bytesTotal)
    {
        var total = Math.Max(bytesTotal, 0);
        var done = Math.Max(bytesDone, 0);

        return new SyncProgress(phase, total > 0 ? Math.Min(done, total) : done, total);
    }
}
