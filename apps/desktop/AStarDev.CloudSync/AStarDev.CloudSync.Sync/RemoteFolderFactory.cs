using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemoteFolder"/> values.</summary>
public static class RemoteFolderFactory
{
    /// <summary>Creates a folder. A missing name becomes an empty string.</summary>
    public static RemoteFolder Create(RemoteItemId id, string? name, RemoteItemId parentId, RemoteFolderChildren children) => new(id, name?.Trim() ?? string.Empty, parentId, children, Option.None<RemoteFolderVersion>());

    /// <summary>Creates a folder that carries the version the provider returned for it. A missing name becomes an empty string.</summary>
    public static RemoteFolder Create(RemoteItemId id, string? name, RemoteItemId parentId, RemoteFolderChildren children, RemoteFolderVersion version) => new(id, name?.Trim() ?? string.Empty, parentId, children, Option.Some(version));
}
