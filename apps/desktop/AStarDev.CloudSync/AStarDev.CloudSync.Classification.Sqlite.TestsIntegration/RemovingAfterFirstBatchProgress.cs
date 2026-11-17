using AStarDev.CloudSync.Classification;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

internal sealed class RemovingAfterFirstBatchProgress(Func<Task> removeAsync) : IProgress<ReclassifyProgress>
{
    public void Report(ReclassifyProgress value)
    {
        if (value.Batch == 1)
            removeAsync().GetAwaiter().GetResult();
    }
}
