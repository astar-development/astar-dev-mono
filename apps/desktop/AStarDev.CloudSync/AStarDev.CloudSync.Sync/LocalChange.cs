namespace AStarDev.CloudSync.Sync;

/// <summary>One change found by a local scan, relative to the sync baseline (#1399).</summary>
public abstract record LocalChange
{
    private LocalChange()
    {
    }

    /// <summary>The path of the changed item.</summary>
    public abstract RelativeSyncPath Path { get; }

    /// <summary>A new file or folder that is not in the baseline.</summary>
    public sealed record Added : LocalChange
    {
        internal Added(RelativeSyncPath path, LocalItemKind kind, long size, DateTimeOffset lastWriteUtc)
        {
            Path = path;
            Kind = kind;
            Size = size;
            LastWriteUtc = lastWriteUtc;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>File or folder.</summary>
        public LocalItemKind Kind { get; }

        /// <summary>The size in bytes; zero for a folder.</summary>
        public long Size { get; }

        /// <summary>The local last write time, in UTC.</summary>
        public DateTimeOffset LastWriteUtc { get; }
    }

    /// <summary>A file whose content differs from the baseline: its size or write time changed and its SHA-256 differs.</summary>
    public sealed record Modified : LocalChange
    {
        internal Modified(RelativeSyncPath path, long size, DateTimeOffset lastWriteUtc, ContentHash hash)
        {
            Path = path;
            Size = size;
            LastWriteUtc = lastWriteUtc;
            Hash = hash;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>The new size in bytes.</summary>
        public long Size { get; }

        /// <summary>The new local last write time, in UTC.</summary>
        public DateTimeOffset LastWriteUtc { get; }

        /// <summary>The new local SHA-256.</summary>
        public ContentHash Hash { get; }
    }

    /// <summary>An item in the baseline that is no longer on disk.</summary>
    public sealed record Deleted : LocalChange
    {
        internal Deleted(RelativeSyncPath path, RemoteItemId itemId)
        {
            Path = path;
            ItemId = itemId;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }

        /// <summary>The remote item the baseline linked to the path.</summary>
        public RemoteItemId ItemId { get; }
    }

    /// <summary>A file that changed within the last two seconds, so it may still be written; it is picked up on the next pass.</summary>
    public sealed record Deferred : LocalChange
    {
        internal Deferred(RelativeSyncPath path)
        {
            Path = path;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }
    }

    /// <summary>An item the scan could not read or that clashes with a sibling that differs only in case: the path is then ambiguous and nothing is transferred for it. It is never reported as deleted.</summary>
    public sealed record Unreadable : LocalChange
    {
        internal Unreadable(RelativeSyncPath path)
        {
            Path = path;
        }

        /// <inheritdoc />
        public override RelativeSyncPath Path { get; }
    }
}
