namespace AStarDev.CloudSync.Classification;

/// <summary>How a reclassify run paces itself. Defaults keep it low priority.</summary>
public sealed record ReclassifyOptions
{
    internal ReclassifyOptions(int batchSize, int degreeOfParallelism, TimeSpan delayBetweenBatches, TimeSpan delayAfterContention, TimeProvider timeProvider)
    {
        BatchSize = batchSize;
        DegreeOfParallelism = degreeOfParallelism;
        DelayBetweenBatches = delayBetweenBatches;
        DelayAfterContention = delayAfterContention;
        TimeProvider = timeProvider;
    }

    /// <summary>The number of files written in one store transaction.</summary>
    public int BatchSize { get; }

    /// <summary>How many files are classified at the same time.</summary>
    public int DegreeOfParallelism { get; }

    /// <summary>The pause after each batch, so a running sync is not starved.</summary>
    public TimeSpan DelayBetweenBatches { get; }

    /// <summary>The longer pause after a batch that found an account's lease taken, so the pass or user action holding or waiting for it gets in.</summary>
    public TimeSpan DelayAfterContention { get; }

    /// <summary>The clock the pauses run on.</summary>
    public TimeProvider TimeProvider { get; }
}
