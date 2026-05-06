namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="StorageQuota"/>.</summary>
public static class StorageQuotaFactory
{
    /// <summary>Creates a <see cref="StorageQuota"/> from the given byte counts.</summary>
    public static StorageQuota Create(long totalBytes, long usedBytes) => new(totalBytes, usedBytes);

    /// <summary>Quota with no data — used before the first Graph API refresh.</summary>
    public static StorageQuota Unknown => new(0, 0);
}
