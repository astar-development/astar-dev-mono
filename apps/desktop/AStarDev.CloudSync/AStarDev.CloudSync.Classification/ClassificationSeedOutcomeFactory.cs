namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ClassificationSeedOutcome"/> cases.</summary>
public static class ClassificationSeedOutcomeFactory
{
    /// <summary>Creates the Applied case.</summary>
    public static ClassificationSeedOutcome CreateApplied() => new ClassificationSeedOutcome.Applied();

    /// <summary>Creates the AlreadyCurrent case.</summary>
    public static ClassificationSeedOutcome CreateAlreadyCurrent() => new ClassificationSeedOutcome.AlreadyCurrent();

    /// <summary>Creates the Failed case.</summary>
    /// <param name="failure">Why the run failed.</param>
    public static ClassificationSeedOutcome CreateFailed(ClassificationFailure failure) => new ClassificationSeedOutcome.Failed(failure);
}
