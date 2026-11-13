namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadedFile"/> values.</summary>
public static class UploadedFileFactory
{
    /// <summary>Creates the result. A negative size becomes zero.</summary>
    public static UploadedFile Create(RemoteItemId id, ContentTag cTag, EntityTag eTag, long size, ContentHash hash) => new(id, cTag, eTag, Math.Max(size, 0), hash);
}
