namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public sealed class SnapshotMismatchException : Exception
{
    public SnapshotMismatchException()
    {
    }

    public SnapshotMismatchException(string message) : base(message)
    {
    }

    public SnapshotMismatchException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
