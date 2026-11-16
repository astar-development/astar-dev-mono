namespace AStarDev.CloudSync.Classification;

/// <summary>How a reclassify run paces itself. Defaults keep it low priority.</summary>
public sealed record ReclassifyOptions
{
    internal ReclassifyOptions(int batchSize, int degreeOfParallelism, TimeSpan delayBetweenBatches)
    {
        BatchSize = batchSize;
        DegreeOfParallelism = degreeOfParallelism;
        DelayBetweenBatches = delayBetweenBatches;
    }

    /// <summary>The number of files written in one store transaction.</summary>
    public int BatchSize { get; }

    /// <summary>How many files are classified at the same time.</summary>
    public int DegreeOfParallelism { get; }

    /// <summary>The pause after each batch, so a running sync is not starved.</summary>
    public TimeSpan DelayBetweenBatches { get; }
}
