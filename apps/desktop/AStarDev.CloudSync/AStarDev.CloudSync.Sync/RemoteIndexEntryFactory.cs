namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RemoteIndexEntry"/> values.</summary>
public static class RemoteIndexEntryFactory
{
    /// <summary>Creates an entry.</summary>
    /// <param name="path">The item's path relative to the sync root.</param>
    /// <param name="item">The item.</param>
    /// <returns>The entry.</returns>
    public static RemoteIndexEntry Create(RelativeSyncPath path, RemoteItemSnapshot item) => new(path, item);
}
