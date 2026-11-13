namespace AStarDev.CloudSync.Sync;

/// <summary>The download pass settings (#1400). The concurrency becomes a per-account setting in #1404.</summary>
public sealed record DownloadPassOptions
{
    internal DownloadPassOptions(int maxConcurrentDownloads, long freeSpaceMarginBytes, int baselineChunkSize)
    {
        MaxConcurrentDownloads = maxConcurrentDownloads;
        FreeSpaceMarginBytes = freeSpaceMarginBytes;
        BaselineChunkSize = baselineChunkSize;
    }

    /// <summary>The most downloads that run at once for one account.</summary>
    public int MaxConcurrentDownloads { get; }

    /// <summary>The free space that must remain on the volume after a file is downloaded.</summary>
    public long FreeSpaceMarginBytes { get; }

    /// <summary>How many downloads run before their baseline rows are written in one batch.</summary>
    public int BaselineChunkSize { get; }
}
