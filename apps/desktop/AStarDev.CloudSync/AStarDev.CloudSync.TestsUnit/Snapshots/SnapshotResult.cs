namespace AStarDev.CloudSync.TestsUnit.Snapshots;

internal abstract record SnapshotResult
{
    private SnapshotResult()
    {
    }

    internal sealed record Match : SnapshotResult;

    internal sealed record Mismatch(FileInfo Actual, FileInfo Diff, int DifferingPixels) : SnapshotResult;

    internal sealed record MissingBaseline(FileInfo Actual) : SnapshotResult;
}

internal static class SnapshotResultFactory
{
    public static SnapshotResult CreateMatch() => new SnapshotResult.Match();

    public static SnapshotResult CreateMismatch(FileInfo actual, FileInfo diff, int differingPixels) => new SnapshotResult.Mismatch(actual, diff, differingPixels);

    public static SnapshotResult CreateMissingBaseline(FileInfo actual) => new SnapshotResult.MissingBaseline(actual);
}
