namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="LocalChange"/> cases.</summary>
public static class LocalChangeFactory
{
    /// <summary>Creates the <see cref="LocalChange.Added"/> case. A negative size becomes zero and the time is converted to UTC.</summary>
    /// <param name="path">The item's path.</param>
    /// <param name="kind">File or folder.</param>
    /// <param name="size">The size in bytes.</param>
    /// <param name="lastWriteUtc">The local last write time.</param>
    public static LocalChange CreateAdded(RelativeSyncPath path, LocalItemKind kind, long size, DateTimeOffset lastWriteUtc) => new LocalChange.Added(path, kind, Math.Max(size, 0), lastWriteUtc.ToUniversalTime());

    /// <summary>Creates the <see cref="LocalChange.Modified"/> case. A negative size becomes zero and the time is converted to UTC.</summary>
    /// <param name="path">The file's path.</param>
    /// <param name="size">The new size in bytes.</param>
    /// <param name="lastWriteUtc">The new local last write time.</param>
    /// <param name="hash">The new local SHA-256.</param>
    public static LocalChange CreateModified(RelativeSyncPath path, long size, DateTimeOffset lastWriteUtc, ContentHash hash) => new LocalChange.Modified(path, Math.Max(size, 0), lastWriteUtc.ToUniversalTime(), hash);

    /// <summary>Creates the <see cref="LocalChange.Deleted"/> case.</summary>
    /// <param name="path">The path that is gone.</param>
    /// <param name="itemId">The remote item the baseline linked to it.</param>
    public static LocalChange CreateDeleted(RelativeSyncPath path, RemoteItemId itemId) => new LocalChange.Deleted(path, itemId);

    /// <summary>Creates the <see cref="LocalChange.Deferred"/> case.</summary>
    /// <param name="path">The file's path.</param>
    public static LocalChange CreateDeferred(RelativeSyncPath path) => new LocalChange.Deferred(path);

    /// <summary>Creates the <see cref="LocalChange.Unreadable"/> case.</summary>
    /// <param name="path">The item's path.</param>
    public static LocalChange CreateUnreadable(RelativeSyncPath path) => new LocalChange.Unreadable(path);
}
