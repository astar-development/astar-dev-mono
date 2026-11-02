namespace AStarDev.OneDriveSyncClient.Dashboard.Throughput;

/// <summary>One bar of the throughput graph.</summary>
/// <param name="Bytes">Bytes transferred in the bucket.</param>
/// <param name="HeightFraction">Bar height relative to the busiest bucket, from 0 to 1.</param>
/// <param name="Tooltip">Text describing the bucket for display on hover.</param>
public sealed record ThroughputBar(long Bytes, double HeightFraction, string Tooltip);
