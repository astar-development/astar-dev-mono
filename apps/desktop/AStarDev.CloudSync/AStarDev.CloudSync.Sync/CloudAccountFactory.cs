using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="CloudAccount"/> values.</summary>
public static class CloudAccountFactory
{
    /// <summary>Creates an account. Missing text is normalised to empty strings and negative counts to zero.</summary>
    public static CloudAccount Create(CloudAccountIdentity identity, StorageUsage storage, int folderCount, int fileCount, SyncStatus status, Option<DateTimeOffset> lastSync) => Create(identity, storage, folderCount, fileCount, status, lastSync, AccountSignInStateFactory.CreateSignedIn());

    /// <summary>Creates an account with its sign-in state. Missing text is normalised to empty strings and negative counts to zero.</summary>
    public static CloudAccount Create(CloudAccountIdentity identity, StorageUsage storage, int folderCount, int fileCount, SyncStatus status, Option<DateTimeOffset> lastSync, AccountSignInState signIn) => Create(identity, storage, Option.Some(folderCount), Option.Some(fileCount), status, lastSync, signIn);

    /// <summary>Creates an account whose counts may be unknown (none). Missing text is normalised to empty strings and negative counts to zero.</summary>
    public static CloudAccount Create(CloudAccountIdentity identity, StorageUsage storage, Option<int> folderCount, Option<int> fileCount, SyncStatus status, Option<DateTimeOffset> lastSync, AccountSignInState signIn) => new(identity.Id, identity.DisplayName ?? string.Empty, identity.Email ?? string.Empty, identity.Provider, storage, folderCount.Map(count => Math.Max(count, 0)), fileCount.Map(count => Math.Max(count, 0)), status, lastSync, signIn);
}
