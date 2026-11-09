
namespace AStarDev.CloudSync.Overview;

/// <summary>Creates <see cref="ThroughputSample"/> values.</summary>
public static class ThroughputSampleFactory
{
    /// <summary>Creates a throughput sample. A negative rate is normalised to zero.</summary>
    public static ThroughputSample Create(DateTimeOffset at, long bytesPerSecond) => new(at, Math.Max(bytesPerSecond, 0));
}
