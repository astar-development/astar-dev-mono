namespace AStarDev.CloudSync.Sync;

/// <summary>How much of a transfer is done.</summary>
public sealed record TransferProgress
{
    internal TransferProgress(long bytesDone, long bytesTotal)
    {
        BytesDone = bytesDone;
        BytesTotal = bytesTotal;
    }

    /// <summary>Bytes transferred so far.</summary>
    public long BytesDone { get; }

    /// <summary>The total bytes, or zero when unknown.</summary>
    public long BytesTotal { get; }
}
