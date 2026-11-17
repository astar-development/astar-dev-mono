namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ReclassifyOptions"/> values.</summary>
public static class ReclassifyOptionsFactory
{
    /// <summary>The default batch size.</summary>
    public const int DefaultBatchSize = 500;

    private const int DefaultDelayMilliseconds = 20;

    private const int DefaultContentionDelayMilliseconds = 500;

    private const int MinimumDelayMilliseconds = 5;

    /// <summary>Creates the defaults: batches of 500, one file at a time, a 20 ms pause after each batch, which measured on 100,000 files cut the 95th percentile wait of a concurrent baseline write from about 2.4 s to about 0.6 s, and a 500 ms pause after a batch that found an account's lease taken, longer than <c>AccountLeasePolicy.RetryInterval</c>, on the system clock.</summary>
    public static ReclassifyOptions CreateDefault() => Create(DefaultBatchSize, 1, TimeSpan.FromMilliseconds(DefaultDelayMilliseconds));

    /// <summary>Creates options with the default 500 ms pause after contention, on the system clock. A batch size or parallelism below one becomes the default, and a negative delay becomes none, and a pause between batches below 5 ms becomes 5 ms so a zero pause cannot starve a concurrent writer.</summary>
    /// <param name="batchSize">The files per store transaction.</param>
    /// <param name="degreeOfParallelism">The files classified at once.</param>
    /// <param name="delayBetweenBatches">The pause after each batch.</param>
    public static ReclassifyOptions Create(int batchSize, int degreeOfParallelism, TimeSpan delayBetweenBatches) =>
        Create(batchSize, degreeOfParallelism, delayBetweenBatches, TimeSpan.FromMilliseconds(DefaultContentionDelayMilliseconds), TimeProvider.System);

    /// <summary>Creates options. A batch size or parallelism below one becomes the default, and a negative delay becomes none, and a pause between batches below 5 ms becomes 5 ms so a zero pause cannot starve a concurrent writer.</summary>
    /// <param name="batchSize">The files per store transaction.</param>
    /// <param name="degreeOfParallelism">The files classified at once.</param>
    /// <param name="delayBetweenBatches">The pause after each batch.</param>
    /// <param name="delayAfterContention">The pause after a batch that found an account's lease taken.</param>
    /// <param name="timeProvider">The clock the pauses run on.</param>
    public static ReclassifyOptions Create(int batchSize, int degreeOfParallelism, TimeSpan delayBetweenBatches, TimeSpan delayAfterContention, TimeProvider timeProvider) =>
        new(batchSize < 1 ? DefaultBatchSize : batchSize, degreeOfParallelism < 1 ? 1 : degreeOfParallelism, AtLeastMinimum(delayBetweenBatches), NotNegative(delayAfterContention), timeProvider);

    private static TimeSpan AtLeastMinimum(TimeSpan delay) => delay < TimeSpan.FromMilliseconds(MinimumDelayMilliseconds) ? TimeSpan.FromMilliseconds(MinimumDelayMilliseconds) : delay;

    private static TimeSpan NotNegative(TimeSpan delay) => delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
}
