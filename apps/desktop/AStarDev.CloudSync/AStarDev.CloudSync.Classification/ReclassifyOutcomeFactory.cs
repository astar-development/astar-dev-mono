namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ReclassifyOutcome"/> cases.</summary>
public static class ReclassifyOutcomeFactory
{
    /// <summary>Creates the Completed case.</summary>
    /// <param name="counts">What was done.</param>
    public static ReclassifyOutcome CreateCompleted(ReclassifyCounts counts) => new ReclassifyOutcome.Completed(counts);

    /// <summary>Creates the Cancelled case.</summary>
    /// <param name="counts">What was done.</param>
    public static ReclassifyOutcome CreateCancelled(ReclassifyCounts counts) => new ReclassifyOutcome.Cancelled(counts);

    /// <summary>Creates the Failed case.</summary>
    /// <param name="counts">What was done.</param>
    public static ReclassifyOutcome CreateFailed(ReclassifyCounts counts) => new ReclassifyOutcome.Failed(counts);

    /// <summary>Creates the AlreadyRunning case.</summary>
    public static ReclassifyOutcome CreateAlreadyRunning() => new ReclassifyOutcome.AlreadyRunning();
}
