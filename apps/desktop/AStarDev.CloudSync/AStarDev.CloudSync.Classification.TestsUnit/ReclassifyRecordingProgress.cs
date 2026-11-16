namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class ReclassifyRecordingProgress : IProgress<ReclassifyProgress>
{
    private readonly List<(int FilesDone, int Batch)> reports = [];

    internal IReadOnlyList<(int FilesDone, int Batch)> Reports => reports;

    public void Report(ReclassifyProgress value)
    {
        lock (reports)
            reports.Add((value.FilesDone, value.Batch));
    }
}
