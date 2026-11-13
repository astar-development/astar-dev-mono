using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncBaselineEntry"/> values.</summary>
public static class SyncBaselineEntryFactory
{
    /// <summary>Creates an entry.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="path">The item's path below the sync root.</param>
    /// <param name="itemId">The remote item id.</param>
    /// <param name="kind">File or folder state.</param>
    public static SyncBaselineEntry Create(AccountId accountId, RelativeSyncPath path, RemoteItemId itemId, SyncBaselineKind kind) => new(accountId, path, itemId, kind);
}
