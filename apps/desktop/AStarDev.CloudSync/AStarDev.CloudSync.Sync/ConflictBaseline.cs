namespace AStarDev.CloudSync.Sync;

/// <summary>What both sides agreed on at the last sync of one file. It is the classifier's own input until the sync baseline table (#1399) exists.</summary>
public sealed record ConflictBaseline
{
    internal ConflictBaseline(ContentHash localHash, ContentTag remoteTag)
    {
        LocalHash = localHash;
        RemoteTag = remoteTag;
    }

    /// <summary>The local SHA-256 at sync time.</summary>
    public ContentHash LocalHash { get; }

    /// <summary>The cloud content tag at sync time.</summary>
    public ContentTag RemoteTag { get; }
}
