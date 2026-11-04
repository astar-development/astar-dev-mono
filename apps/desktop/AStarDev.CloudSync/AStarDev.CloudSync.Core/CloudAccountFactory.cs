using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Core;

/// <summary>Creates <see cref="CloudAccount"/> values.</summary>
public static class CloudAccountFactory
{
    /// <summary>Creates an account. Missing text is normalised to empty strings and negative counts to zero.</summary>
    public static CloudAccount Create(AccountId id, string displayName, string email, CloudProvider provider, StorageUsage storage, int folderCount, int fileCount, SyncStatus status, Option<DateTimeOffset> lastSync) => new(id, displayName ?? string.Empty, email ?? string.Empty, provider, storage, Math.Max(folderCount, 0), Math.Max(fileCount, 0), status, lastSync);
}
