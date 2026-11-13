namespace AStarDev.CloudSync.Sync;

/// <summary>What the provider returned for an uploaded file (#1401).</summary>
public sealed record UploadedFile
{
    internal UploadedFile(RemoteItemId id, ContentTag cTag, EntityTag eTag, long size, ContentHash hash)
    {
        Id = id;
        CTag = cTag;
        ETag = eTag;
        Size = size;
        Hash = hash;
    }

    /// <summary>The file's id.</summary>
    public RemoteItemId Id { get; }

    /// <summary>The content tag of the uploaded version.</summary>
    public ContentTag CTag { get; }

    /// <summary>The entity tag.</summary>
    public EntityTag ETag { get; }

    /// <summary>The size in bytes.</summary>
    public long Size { get; }

    /// <summary>The SHA-256 of the bytes sent, in the same form as the local scan's hash, so the baseline row matches the next scan.</summary>
    public ContentHash Hash { get; }
}
