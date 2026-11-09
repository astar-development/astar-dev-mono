namespace AStarDev.CloudSync.Sync;

/// <summary>What the provider returned for an uploaded file (#1401).</summary>
public sealed record UploadedFile
{
    internal UploadedFile(RemoteItemId id, string cTag, string eTag, long size)
    {
        Id = id;
        CTag = cTag;
        ETag = eTag;
        Size = size;
    }

    /// <summary>The file's id.</summary>
    public RemoteItemId Id { get; }

    /// <summary>The content tag of the uploaded version.</summary>
    public string CTag { get; }

    /// <summary>The entity tag.</summary>
    public string ETag { get; }

    /// <summary>The size in bytes.</summary>
    public long Size { get; }
}
