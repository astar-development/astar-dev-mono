namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadedFile"/> values.</summary>
public static class UploadedFileFactory
{
    /// <summary>Creates the result. Missing tags become empty strings and a negative size zero.</summary>
    public static UploadedFile Create(RemoteItemId id, string? cTag, string? eTag, long size) => new(id, cTag ?? string.Empty, eTag ?? string.Empty, Math.Max(size, 0));
}
