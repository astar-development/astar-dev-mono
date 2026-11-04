using AStarDev.CloudSync.Core.Accounts;

namespace AStarDev.CloudSync.Domain.Accounts;

/// <summary>Creates <see cref="StorageUsage"/> values.</summary>
public static class StorageUsageFactory
{
    /// <summary>Creates a storage usage value. Negative sizes are normalised to zero.</summary>
    public static StorageUsage Create(long usedBytes, long totalBytes) => new(Math.Max(usedBytes, 0), Math.Max(totalBytes, 0));
}
