using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="AccountSyncRoot"/> values.</summary>
public static class AccountSyncRootFactory
{
    /// <summary>Creates an account's sync root entry.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="root">The account's sync root.</param>
    public static AccountSyncRoot Create(AccountId accountId, SyncRoot root) => new(accountId, root);
}
