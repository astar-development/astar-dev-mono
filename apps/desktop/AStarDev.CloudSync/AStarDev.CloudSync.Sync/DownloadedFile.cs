using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>What the provider reported about a downloaded file (#1400).</summary>
public sealed record DownloadedFile
{
    internal DownloadedFile(RemoteItemId id, ContentTag cTag, long size, Option<ContentHash> hash)
    {
        Id = id;
        CTag = cTag;
        Size = size;
        Hash = hash;
    }

    /// <summary>The file's id.</summary>
    public RemoteItemId Id { get; }

    /// <summary>The content tag of the downloaded version.</summary>
    public ContentTag CTag { get; }

    /// <summary>The size in bytes.</summary>
    public long Size { get; }

    /// <summary>The provider's content hash, when it has one.</summary>
    public Option<ContentHash> Hash { get; }
}
