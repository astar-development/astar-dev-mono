namespace AStarDev.CloudSync.Classification;

/// <summary>How a seeding run ended.</summary>
public abstract record ClassificationSeedOutcome
{
    private ClassificationSeedOutcome()
    {
    }

    /// <summary>The seed was written to the stores and its version recorded.</summary>
    public sealed record Applied : ClassificationSeedOutcome
    {
        internal Applied()
        {
        }
    }

    /// <summary>The recorded version equals the seed version, so nothing was written.</summary>
    public sealed record AlreadyCurrent : ClassificationSeedOutcome
    {
        internal AlreadyCurrent()
        {
        }
    }

    /// <summary>The seed was not fully applied and its version was not recorded.</summary>
    public sealed record Failed : ClassificationSeedOutcome
    {
        internal Failed(ClassificationFailure failure) => Failure = failure;

        /// <summary>Why the run failed.</summary>
        public ClassificationFailure Failure { get; }
    }
}
