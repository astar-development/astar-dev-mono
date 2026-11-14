namespace AStarDev.CloudSync.Sync;

/// <summary>What a download pass keeps free and how it batches baseline writes. How many files it downloads at once is the account's <see cref="TransferConcurrency"/>.</summary>
public sealed record DownloadPassOptions
{
    internal DownloadPassOptions(long freeSpaceMarginBytes, int baselineChunkSize)
    {
        FreeSpaceMarginBytes = freeSpaceMarginBytes;
        BaselineChunkSize = baselineChunkSize;
    }

    /// <summary>The free disk space a download must leave, in bytes.</summary>
    public long FreeSpaceMarginBytes { get; }

    /// <summary>How many files are downloaded before their baseline rows are written together.</summary>
    public int BaselineChunkSize { get; }
}
