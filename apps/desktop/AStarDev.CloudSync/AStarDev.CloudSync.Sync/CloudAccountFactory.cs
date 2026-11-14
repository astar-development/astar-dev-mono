using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="CloudAccount"/> values.</summary>
public static class CloudAccountFactory
{
    /// <summary>Creates an account. Missing text is normalised to empty strings and negative counts to zero.</summary>
    public static CloudAccount Create(AccountId id, string displayName, string email, CloudProvider provider, StorageUsage storage, int folderCount, int fileCount, SyncStatus status, Option<DateTimeOffset> lastSync) => Create(id, displayName, email, provider, storage, folderCount, fileCount, status, lastSync, AccountSignInStateFactory.CreateSignedIn());

    /// <summary>Creates an account with its sign-in state. Missing text is normalised to empty strings and negative counts to zero.</summary>
    public static CloudAccount Create(AccountId id, string displayName, string email, CloudProvider provider, StorageUsage storage, int folderCount, int fileCount, SyncStatus status, Option<DateTimeOffset> lastSync, AccountSignInState signIn) => Create(id, displayName, email, provider, storage, Option.Some(folderCount), Option.Some(fileCount), status, lastSync, signIn);

    /// <summary>Creates an account whose counts may be unknown (none). Missing text is normalised to empty strings and negative counts to zero.</summary>
    public static CloudAccount Create(AccountId id, string displayName, string email, CloudProvider provider, StorageUsage storage, Option<int> folderCount, Option<int> fileCount, SyncStatus status, Option<DateTimeOffset> lastSync, AccountSignInState signIn) => new(id, displayName ?? string.Empty, email ?? string.Empty, provider, storage, folderCount.Map(count => Math.Max(count, 0)), fileCount.Map(count => Math.Max(count, 0)), status, lastSync, signIn);
}
