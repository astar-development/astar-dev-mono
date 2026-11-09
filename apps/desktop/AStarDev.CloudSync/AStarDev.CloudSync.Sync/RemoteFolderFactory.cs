namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemoteFolder"/> values.</summary>
public static class RemoteFolderFactory
{
    /// <summary>Creates a folder. A missing name becomes an empty string.</summary>
    public static RemoteFolder Create(RemoteItemId id, string? name, RemoteItemId parentId, RemoteFolderChildren children) => new(id, name?.Trim() ?? string.Empty, parentId, children);
}
