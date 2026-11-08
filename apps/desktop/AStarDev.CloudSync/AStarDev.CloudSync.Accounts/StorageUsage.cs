namespace AStarDev.CloudSync.Accounts;

/// <summary>How much of an account's cloud storage is used.</summary>
public sealed record StorageUsage
{
    internal StorageUsage(long usedBytes, long totalBytes)
    {
        UsedBytes = usedBytes;
        TotalBytes = totalBytes;
    }

    /// <summary>Bytes used.</summary>
    public long UsedBytes { get; }

    /// <summary>Bytes available in total.</summary>
    public long TotalBytes { get; }
}
