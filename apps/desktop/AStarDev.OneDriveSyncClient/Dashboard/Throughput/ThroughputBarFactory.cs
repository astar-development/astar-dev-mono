namespace AStarDev.OneDriveSyncClient.Dashboard.Throughput;

/// <summary>Creates <see cref="ThroughputBar"/> instances.</summary>
public static class ThroughputBarFactory
{
    /// <summary>Creates a bar for <paramref name="bytes"/> scaled against <paramref name="peakBytes"/>. A non-positive peak or negative byte count yields an empty bar.</summary>
    public static ThroughputBar Create(long bytes, long peakBytes, string tooltip)
    {
        long safeBytes = Math.Max(0, bytes);
        double fraction = peakBytes <= 0 ? 0 : Math.Min(1.0, (double)safeBytes / peakBytes);

        return new ThroughputBar(safeBytes, fraction, tooltip);
    }
}
