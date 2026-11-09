using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadedFile"/> values.</summary>
public static class DownloadedFileFactory
{
    /// <summary>Creates the result. A missing cTag becomes an empty string and a negative size zero.</summary>
    public static DownloadedFile Create(RemoteItemId id, string? cTag, long size, Option<string> hash) => throw new NotImplementedException($"{id}{cTag}{size}{hash}");
}
