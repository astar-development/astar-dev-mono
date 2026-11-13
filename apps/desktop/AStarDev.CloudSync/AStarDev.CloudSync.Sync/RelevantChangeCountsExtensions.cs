namespace AStarDev.CloudSync.Sync;

/// <summary>Combines <see cref="RelevantChangeCounts"/> values.</summary>
public static class RelevantChangeCountsExtensions
{
    /// <summary>Adds two sets of counts.</summary>
    /// <param name="counts">The first set.</param>
    /// <param name="other">The second set.</param>
    public static RelevantChangeCounts Plus(this RelevantChangeCounts counts, RelevantChangeCounts other) =>
        RelevantChangeCountsFactory.Create(counts.Added + other.Added, counts.Modified + other.Modified, counts.Moved + other.Moved, counts.Deleted + other.Deleted);

    /// <summary>The total of all four counts.</summary>
    /// <param name="counts">The counts.</param>
    public static int Total(this RelevantChangeCounts counts) => counts.Added + counts.Modified + counts.Moved + counts.Deleted;
}
