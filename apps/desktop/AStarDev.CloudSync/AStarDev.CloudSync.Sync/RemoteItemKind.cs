using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>What a remote item is.</summary>
public abstract record RemoteItemKind
{
    private RemoteItemKind()
    {
    }

    /// <summary>A file.</summary>
    public sealed record File : RemoteItemKind
    {
        internal File(long size, Option<string> hash, string cTag)
        {
            Size = size;
            Hash = hash;
            CTag = cTag;
        }

        /// <summary>The size in bytes.</summary>
        public long Size { get; }

        /// <summary>The provider's content hash, when it has one.</summary>
        public Option<string> Hash { get; }

        /// <summary>The content tag; it changes only when the content changes.</summary>
        public string CTag { get; }
    }

    /// <summary>A folder.</summary>
    public sealed record Folder : RemoteItemKind
    {
        internal Folder()
        {
        }
    }

    /// <summary>The item was deleted on the provider.</summary>
    public sealed record Deleted : RemoteItemKind
    {
        internal Deleted()
        {
        }
    }
}
