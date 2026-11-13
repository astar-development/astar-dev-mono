namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RelevantChangeCounts"/> values.</summary>
public static class RelevantChangeCountsFactory
{
    /// <summary>The counts of an update that found nothing relevant.</summary>
    public static RelevantChangeCounts None { get; } = new(0, 0, 0, 0);

    /// <summary>Creates the counts; a negative count becomes zero.</summary>
    public static RelevantChangeCounts Create(int added, int modified, int moved, int deleted) => new(Math.Max(added, 0), Math.Max(modified, 0), Math.Max(moved, 0), Math.Max(deleted, 0));
}
