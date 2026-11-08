
namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="ActivityKind"/> cases.</summary>
public static class ActivityKindFactory
{
    /// <summary>Creates a detecting-changes entry.</summary>
    public static ActivityKind CreateDetectingChanges() => new ActivityKind.DetectingChanges();

    /// <summary>Creates an uploaded entry. Negative values are normalised to zero.</summary>
    public static ActivityKind CreateUploaded(int fileCount, long bytes) => new ActivityKind.Uploaded(Math.Max(fileCount, 0), Math.Max(bytes, 0));

    /// <summary>Creates a downloaded entry. A negative count is normalised to zero.</summary>
    public static ActivityKind CreateDownloaded(int fileCount) => new ActivityKind.Downloaded(Math.Max(fileCount, 0));

    /// <summary>Creates a conflict entry. A missing file name is normalised to an empty string.</summary>
    public static ActivityKind CreateConflictDetected(string fileName) => new ActivityKind.ConflictDetected(fileName ?? string.Empty);

    /// <summary>Creates an authenticated entry.</summary>
    public static ActivityKind CreateAuthenticated() => new ActivityKind.Authenticated();
}
