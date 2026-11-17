namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ReclassifyFreshness"/> cases.</summary>
public static class ReclassifyFreshnessFactory
{
    /// <summary>Creates the UpToDate case.</summary>
    public static ReclassifyFreshness CreateUpToDate() => new ReclassifyFreshness.UpToDate();

    /// <summary>Creates the Stale case.</summary>
    public static ReclassifyFreshness CreateStale() => new ReclassifyFreshness.Stale();
}
