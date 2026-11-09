using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>An account and the sync root it owns.</summary>
public sealed record AccountSyncRoot
{
    internal AccountSyncRoot(AccountId accountId, SyncRoot root)
    {
        AccountId = accountId;
        Root = root;
    }

    /// <summary>The account.</summary>
    public AccountId AccountId { get; }

    /// <summary>The account's sync root.</summary>
    public SyncRoot Root { get; }
}
