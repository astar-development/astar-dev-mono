namespace AStarDev.CloudSync.Sync;

/// <summary>How far an account's sync run has got: the phase, and the bytes done out of the bytes the phase has to move (#1563).</summary>
public sealed record SyncProgress
{
    internal SyncProgress(SyncPhase phase, long bytesDone, long bytesTotal)
    {
        Phase = phase;
        BytesDone = bytesDone;
        BytesTotal = bytesTotal;
    }

    /// <summary>The phase the run is in.</summary>
    public SyncPhase Phase { get; }

    /// <summary>Bytes moved so far in this phase.</summary>
    public long BytesDone { get; }

    /// <summary>The bytes this phase has to move, or zero when it moves none or the total is unknown.</summary>
    public long BytesTotal { get; }
}
