using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>One synced item as both sides last agreed on it (#1399). The path is relative to the account's sync root and never empty.</summary>
public sealed record SyncBaselineEntry
{
    internal SyncBaselineEntry(AccountId accountId, RelativeSyncPath path, RemoteItemId itemId, SyncBaselineKind kind)
    {
        AccountId = accountId;
        Path = path;
        ItemId = itemId;
        Kind = kind;
    }

    /// <summary>The account.</summary>
    public AccountId AccountId { get; }

    /// <summary>The item's path below the sync root, equal ignoring case.</summary>
    public RelativeSyncPath Path { get; }

    /// <summary>The remote item id.</summary>
    public RemoteItemId ItemId { get; }

    /// <summary>File or folder state at sync time.</summary>
    public SyncBaselineKind Kind { get; }
}
