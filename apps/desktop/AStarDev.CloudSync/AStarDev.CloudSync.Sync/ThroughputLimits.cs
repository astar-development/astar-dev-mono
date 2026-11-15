namespace AStarDev.CloudSync.Sync;

/// <summary>How transfer throughput is sampled and kept (#1767).</summary>
public static class ThroughputLimits
{
    /// <summary>A sample is written at least this often while bytes are moving, and when a phase finishes.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(10);

    /// <summary>How long samples are kept: the graph shows two hours and the bytes moved today need the whole day.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);
}
