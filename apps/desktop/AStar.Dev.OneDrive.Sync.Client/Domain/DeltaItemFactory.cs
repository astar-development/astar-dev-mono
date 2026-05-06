using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="DeltaItem"/>.</summary>
public static class DeltaItemFactory
{
    /// <summary>Creates a <see cref="DeltaItem"/> representing a remote drive item.</summary>
    public static DeltaItem Create(OneDriveItemId id, string driveId, OneDriveFolderId? parentId, ItemPath path, bool isFolder, bool isDeleted, long size, DateTimeOffset? lastModified, string? downloadUrl, VersionInfo versionInfo) => new(id, driveId, parentId, path, isFolder, isDeleted, size, lastModified, downloadUrl, versionInfo);
}
