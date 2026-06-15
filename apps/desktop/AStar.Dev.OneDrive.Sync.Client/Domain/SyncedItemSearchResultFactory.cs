using AStar.Dev.OneDrive.Sync.Client.Data.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="SyncedItemSearchResult"/>.</summary>
public static class SyncedItemSearchResultFactory
{
    /// <summary>Creates a <see cref="SyncedItemSearchResult"/> from the provided field values.</summary>
    public static SyncedItemSearchResult Create(int id, AccountId accountId, OneDriveItemId remoteItemId, string remotePath, string localPath, DateTimeOffset remoteModifiedAt, long? sizeInBytes, IReadOnlyList<string> tagNames) => new(id, accountId, remoteItemId, remotePath, localPath, remoteModifiedAt, sizeInBytes, tagNames);
}
