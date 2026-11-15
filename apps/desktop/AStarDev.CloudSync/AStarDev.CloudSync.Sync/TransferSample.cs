namespace AStarDev.CloudSync.Sync;

/// <summary>The bytes sync moved, across every account, in one sampling window that ended at <see cref="At"/> (#1767).</summary>
public sealed record TransferSample
{
    internal TransferSample(DateTimeOffset at, long bytes, TimeSpan elapsed)
    {
        At = at;
        Bytes = bytes;
        Elapsed = elapsed;
    }

    /// <summary>When the window ended.</summary>
    public DateTimeOffset At { get; }

    /// <summary>The bytes uploaded and downloaded in the window.</summary>
    public long Bytes { get; }

    /// <summary>How long the window lasted, from its first transferred bytes to its end.</summary>
    public TimeSpan Elapsed { get; }
}
