using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="CloudAccount"/> values.</summary>
public static class CloudAccountFactory
{
    /// <summary>Creates an account. Missing text is normalised to empty strings and negative counts to zero.</summary>
    public static CloudAccount Create(AccountId id, string displayName, string email, CloudProvider provider, StorageUsage storage, int folderCount, int fileCount, SyncStatus status, Option<DateTimeOffset> lastSync) => new(id, displayName ?? string.Empty, email ?? string.Empty, provider, storage, Math.Max(folderCount, 0), Math.Max(fileCount, 0), status, lastSync, AccountSignInStateFactory.CreateSignedIn());

    /// <summary>Creates a copy of an account with the given sign-in state.</summary>
    public static CloudAccount CreateWithSignIn(CloudAccount account, AccountSignInState signIn) => new(account.Id, account.DisplayName, account.Email, account.Provider, account.Storage, account.FolderCount, account.FileCount, account.Status, account.LastSync, signIn);
}
