namespace AStarDev.CloudSync.Sync;

/// <summary>What a baseline row records about a synced item (#1399).</summary>
public abstract record SyncBaselineKind
{
    private SyncBaselineKind()
    {
    }

    /// <summary>A file as both sides last agreed on it.</summary>
    public sealed record File : SyncBaselineKind
    {
        internal File(long size, DateTimeOffset lastWriteUtc, ContentHash hash, ContentTag cTag, EntityTag eTag)
        {
            Size = size;
            LastWriteUtc = lastWriteUtc;
            Hash = hash;
            CTag = cTag;
            ETag = eTag;
        }

        /// <summary>The size in bytes at sync time.</summary>
        public long Size { get; }

        /// <summary>The local last write time at sync time, in UTC. A later scan compares it exactly, never with the remote time.</summary>
        public DateTimeOffset LastWriteUtc { get; }

        /// <summary>The local SHA-256 of the content at sync time.</summary>
        public ContentHash Hash { get; }

        /// <summary>The remote content tag at sync time.</summary>
        public ContentTag CTag { get; }

        /// <summary>The remote entity tag at sync time; an upload sends it as the <c>if-match</c> precondition (#1401).</summary>
        public EntityTag ETag { get; }
    }

    /// <summary>A folder.</summary>
    public sealed record Folder : SyncBaselineKind
    {
        internal Folder()
        {
        }
    }
}
