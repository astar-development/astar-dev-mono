namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class ReclassifyRecordingProgress : IProgress<ReclassifyProgress>
{
    private readonly List<(int FilesDone, int Batch, int FilesSkipped)> reports = [];

    internal IReadOnlyList<(int FilesDone, int Batch, int FilesSkipped)> Reports => reports;

    public void Report(ReclassifyProgress value)
    {
        lock (reports)
            reports.Add((value.FilesDone, value.Batch, value.FilesSkipped));
    }
}
