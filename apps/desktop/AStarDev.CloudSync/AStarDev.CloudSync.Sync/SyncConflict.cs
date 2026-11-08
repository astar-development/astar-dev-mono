
namespace AStarDev.CloudSync.Sync;

/// <summary>A file that changed both locally and in the cloud.</summary>
public sealed record SyncConflict
{
    internal SyncConflict(SyncPairId syncPairId, string fileName, DateTimeOffset detectedAt)
    {
        SyncPairId = syncPairId;
        FileName = fileName;
        DetectedAt = detectedAt;
    }

    /// <summary>The sync pair holding the file.</summary>
    public SyncPairId SyncPairId { get; }

    /// <summary>The file's name.</summary>
    public string FileName { get; }

    /// <summary>When the conflict was found.</summary>
    public DateTimeOffset DetectedAt { get; }
}
