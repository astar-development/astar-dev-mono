namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="TransferProgress"/> values.</summary>
public static class TransferProgressFactory
{
    /// <summary>Creates progress. Negative values become zero, and done never exceeds a known total.</summary>
    public static TransferProgress Create(long bytesDone, long bytesTotal) => throw new NotImplementedException($"{bytesDone}{bytesTotal}");
}
