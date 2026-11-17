namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class RemovalRacingProgress(int holdAtBatch) : IProgress<ReclassifyProgress>
{
    internal TaskCompletionSource FirstBatchReported { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal TaskCompletionSource RemovalsDone { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Report(ReclassifyProgress value)
    {
        _ = FirstBatchReported.TrySetResult();
        if (value.Batch == holdAtBatch)
            RemovalsDone.Task.GetAwaiter().GetResult();
    }
}
