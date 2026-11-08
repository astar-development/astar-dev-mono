namespace AStarDev.CloudSync.Sync;

/// <summary>What happened in an activity entry.</summary>
public abstract record ActivityKind
{
    private ActivityKind()
    {
    }

    /// <summary>Scanning the local folders for changes.</summary>
    public sealed record DetectingChanges : ActivityKind;

    /// <summary>Files were uploaded.</summary>
    public sealed record Uploaded(int FileCount, long Bytes) : ActivityKind;

    /// <summary>Files were downloaded.</summary>
    public sealed record Downloaded(int FileCount) : ActivityKind;

    /// <summary>A file changed on both sides.</summary>
    public sealed record ConflictDetected(string FileName) : ActivityKind;

    /// <summary>The account signed in.</summary>
    public sealed record Authenticated : ActivityKind;
}
