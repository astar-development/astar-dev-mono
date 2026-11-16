namespace AStarDev.CloudSync.Sync;

/// <summary>The files an audit found for one account (#1801).</summary>
public sealed record FileAuditCounts
{
    internal FileAuditCounts(long localFiles, long cloudFiles)
    {
        LocalFiles = localFiles;
        CloudFiles = cloudFiles;
    }

    /// <summary>The files in the local sync folder that the selection includes.</summary>
    public long LocalFiles { get; }

    /// <summary>The files in the cloud account that the selection includes.</summary>
    public long CloudFiles { get; }
}
