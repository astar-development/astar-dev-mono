namespace AStarDev.CloudSync.Sync;

/// <summary>The version of a remote folder as the provider reported it when the folder was created or looked up (#1555).</summary>
public sealed record RemoteFolderVersion
{
    internal RemoteFolderVersion(EntityTag eTag, DateTimeOffset lastModified)
    {
        ETag = eTag;
        LastModified = lastModified;
    }

    /// <summary>The folder's entity tag, used as the precondition of a later delete.</summary>
    public EntityTag ETag { get; }

    /// <summary>When the provider last modified the folder.</summary>
    public DateTimeOffset LastModified { get; }
}
