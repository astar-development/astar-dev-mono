using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadedFile"/> values.</summary>
public static class DownloadedFileFactory
{
    /// <summary>Creates the result. A negative size becomes zero.</summary>
    public static DownloadedFile Create(RemoteItemId id, ContentTag cTag, long size, Option<ContentHash> hash) => new(id, cTag, Math.Max(size, 0), hash);
}
