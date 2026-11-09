namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="TransferProgress"/> values.</summary>
public static class TransferProgressFactory
{
    /// <summary>Creates progress. Negative values become zero, and done never exceeds a known total.</summary>
    public static TransferProgress Create(long bytesDone, long bytesTotal) => Normalise(bytesDone, bytesTotal);

    private static TransferProgress Normalise(long bytesDone, long bytesTotal)
    {
        var total = Math.Max(bytesTotal, 0);
        var done = Math.Max(bytesDone, 0);

        return new TransferProgress(total > 0 ? Math.Min(done, total) : done, total);
    }
}
