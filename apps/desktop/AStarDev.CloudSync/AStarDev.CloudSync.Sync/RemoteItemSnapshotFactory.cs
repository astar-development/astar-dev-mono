using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemoteItemSnapshot"/> values.</summary>
public static class RemoteItemSnapshotFactory
{
    /// <summary>Creates a snapshot. A missing name becomes an empty string.</summary>
    public static RemoteItemSnapshot Create(RemoteItemId id, Option<RemoteItemId> parentId, string? name, RemoteItemKind kind, DateTimeOffset lastModified, EntityTag eTag) => throw new NotImplementedException($"{id}{parentId}{name}{kind}{lastModified}{eTag}");
}
