namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadPassOptions"/> values.</summary>
public static class DownloadPassOptionsFactory
{
    private const long DefaultMargin = 100L * 1024 * 1024;
    private const int DefaultChunk = 100;

    /// <summary>Creates the defaults: a 100 MB free-space margin and 100 downloads per baseline batch.</summary>
    public static DownloadPassOptions Create() => new(DefaultMargin, DefaultChunk);

    /// <summary>Creates options. A value below its minimum becomes the minimum (no margin, one per batch).</summary>
    /// <param name="freeSpaceMarginBytes">The free-space margin.</param>
    /// <param name="baselineChunkSize">How many downloads share one baseline batch.</param>
    public static DownloadPassOptions Create(long freeSpaceMarginBytes, int baselineChunkSize) => new(Math.Max(freeSpaceMarginBytes, 0), Math.Max(baselineChunkSize, 1));
}
