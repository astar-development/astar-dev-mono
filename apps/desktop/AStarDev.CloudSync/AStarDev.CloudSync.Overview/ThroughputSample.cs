namespace AStarDev.CloudSync.Overview;

/// <summary>The sync transfer rate at a point in time.</summary>
public sealed record ThroughputSample
{
    internal ThroughputSample(DateTimeOffset at, long bytesPerSecond)
    {
        At = at;
        BytesPerSecond = bytesPerSecond;
    }

    /// <summary>When the rate was measured.</summary>
    public DateTimeOffset At { get; }

    /// <summary>The transfer rate.</summary>
    public long BytesPerSecond { get; }
}
