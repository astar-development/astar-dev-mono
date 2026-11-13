namespace AStarDev.CloudSync.Sync;

/// <summary>Limits for an upload pass. A per-account setting replaces these defaults in #1404.</summary>
public sealed record UploadPassOptions
{
    internal UploadPassOptions(int maxConcurrentUploads)
    {
        MaxConcurrentUploads = maxConcurrentUploads;
    }

    /// <summary>How many files upload at the same time for one account.</summary>
    public int MaxConcurrentUploads { get; }
}
