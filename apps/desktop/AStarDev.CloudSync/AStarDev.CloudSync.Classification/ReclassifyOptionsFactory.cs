namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ReclassifyOptions"/> values.</summary>
public static class ReclassifyOptionsFactory
{
    /// <summary>The default batch size.</summary>
    public const int DefaultBatchSize = 500;

    /// <summary>Creates the defaults: batches of 500, one file at a time, no extra delay.</summary>
    public static ReclassifyOptions CreateDefault() => new(DefaultBatchSize, 1, TimeSpan.Zero);

    /// <summary>Creates options. A batch size or parallelism below one becomes the default, and a negative delay becomes none.</summary>
    /// <param name="batchSize">The files per store transaction.</param>
    /// <param name="degreeOfParallelism">The files classified at once.</param>
    /// <param name="delayBetweenBatches">The pause after each batch.</param>
    public static ReclassifyOptions Create(int batchSize, int degreeOfParallelism, TimeSpan delayBetweenBatches) =>
        new(batchSize < 1 ? DefaultBatchSize : batchSize, degreeOfParallelism < 1 ? 1 : degreeOfParallelism, delayBetweenBatches < TimeSpan.Zero ? TimeSpan.Zero : delayBetweenBatches);
}
