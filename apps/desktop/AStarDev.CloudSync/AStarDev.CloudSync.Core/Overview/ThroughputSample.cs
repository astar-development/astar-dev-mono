namespace AStarDev.CloudSync.Core.Overview;

/// <summary>The sync transfer rate at a point in time.</summary>
/// <param name="At">When the rate was measured.</param>
/// <param name="BytesPerSecond">The transfer rate.</param>
public sealed record ThroughputSample(DateTimeOffset At, long BytesPerSecond);
