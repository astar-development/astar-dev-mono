using AStar.Dev.OneDrive.Sync.Client.Data.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="RemoteItemRef"/>.</summary>
public static class RemoteItemRefFactory
{
    /// <summary>Creates a <see cref="RemoteItemRef"/> identifying a specific remote drive item.</summary>
    public static RemoteItemRef Create(AccountId accountId, OneDriveFolderId folderId, OneDriveItemId remoteItemId) => new(accountId, folderId, remoteItemId);
}
