namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemoteFolderVersion"/> values.</summary>
public static class RemoteFolderVersionFactory
{
    /// <summary>Creates a folder version from the entity tag and modified time the provider returned.</summary>
    public static RemoteFolderVersion Create(EntityTag eTag, DateTimeOffset lastModified) => new(eTag, lastModified);
}
