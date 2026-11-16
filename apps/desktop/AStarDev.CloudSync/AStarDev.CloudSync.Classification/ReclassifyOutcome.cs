namespace AStarDev.CloudSync.Classification;

/// <summary>How a reclassify run ended.</summary>
public abstract record ReclassifyOutcome
{
    private ReclassifyOutcome()
    {
    }

    /// <summary>Every file was classified and written.</summary>
    public sealed record Completed : ReclassifyOutcome
    {
        internal Completed(ReclassifyCounts counts) => Counts = counts;

        /// <summary>What was done.</summary>
        public ReclassifyCounts Counts { get; }
    }

    /// <summary>The caller cancelled. Batches already committed stay; the current batch was not written.</summary>
    public sealed record Cancelled : ReclassifyOutcome
    {
        internal Cancelled(ReclassifyCounts counts) => Counts = counts;

        /// <summary>What was done before the cancel.</summary>
        public ReclassifyCounts Counts { get; }
    }

    /// <summary>The file stream or a batch write failed. Batches already committed stay.</summary>
    public sealed record Failed : ReclassifyOutcome
    {
        internal Failed(ReclassifyCounts counts) => Counts = counts;

        /// <summary>What was done before the failure.</summary>
        public ReclassifyCounts Counts { get; }
    }

    /// <summary>Another run is in progress, so nothing started.</summary>
    public sealed record AlreadyRunning : ReclassifyOutcome
    {
        internal AlreadyRunning()
        {
        }
    }
}
