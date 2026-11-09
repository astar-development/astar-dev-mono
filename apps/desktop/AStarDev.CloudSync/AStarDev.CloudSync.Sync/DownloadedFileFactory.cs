using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadedFile"/> values.</summary>
public static class DownloadedFileFactory
{
    /// <summary>Creates the result. A missing cTag becomes an empty string and a negative size zero.</summary>
    public static DownloadedFile Create(RemoteItemId id, string? cTag, long size, Option<string> hash) => new(id, cTag ?? string.Empty, Math.Max(size, 0), hash);
}
