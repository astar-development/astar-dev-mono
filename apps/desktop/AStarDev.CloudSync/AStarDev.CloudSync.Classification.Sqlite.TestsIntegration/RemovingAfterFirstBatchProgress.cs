using AStarDev.CloudSync.Classification;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

internal sealed class RemovingAfterFirstBatchProgress(Func<Task> removeAsync) : IProgress<ReclassifyProgress>
{
    private int removed;

    public void Report(ReclassifyProgress value)
    {
        if (value.Batch == 1 && Interlocked.Exchange(ref removed, 1) == 0)
            removeAsync().GetAwaiter().GetResult();
    }
}
