using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>What one side (local or cloud) held for a path when a conflict was detected. Nothing here is null: a missing item is <see cref="Absent"/>, a missing hash or tag is none.</summary>
public abstract record SideSnapshot
{
    private SideSnapshot()
    {
    }

    /// <summary>The item does not exist on this side.</summary>
    public sealed record Absent : SideSnapshot
    {
        internal Absent()
        {
        }
    }

    /// <summary>The item exists on this side.</summary>
    public sealed record Present : SideSnapshot
    {
        internal Present(long size, DateTimeOffset lastModified, Option<ContentHash> hash, Option<ContentTag> tag)
        {
            Size = size;
            LastModified = lastModified;
            Hash = hash;
            Tag = tag;
        }

        /// <summary>The size in bytes.</summary>
        public long Size { get; }

        /// <summary>When the item last changed on this side.</summary>
        public DateTimeOffset LastModified { get; }

        /// <summary>The local SHA-256 of the content, when it was computed.</summary>
        public Option<ContentHash> Hash { get; }

        /// <summary>The cloud content tag, when the side is the cloud.</summary>
        public Option<ContentTag> Tag { get; }
    }
}
