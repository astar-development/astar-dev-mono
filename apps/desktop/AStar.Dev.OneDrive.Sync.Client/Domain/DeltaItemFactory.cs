using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Home;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="DeltaItem"/> and its derived types.</summary>
public static class DeltaItemFactory
{
    /// <summary>Creates a <see cref="FileDeltaItem"/>.</summary>
    public static FileDeltaItem CreateFile(OneDriveItemId id, DriveId driveId, OneDriveFolderId? parentId, ItemPath path, long size, DateTimeOffset? lastModified, string? downloadUrl, VersionInfo versionInfo) => new(id, driveId, parentId, path, size, lastModified, downloadUrl, versionInfo);

    /// <summary>Creates a <see cref="FolderDeltaItem"/>.</summary>
    public static FolderDeltaItem CreateFolder(OneDriveItemId id, DriveId driveId, OneDriveFolderId? parentId, ItemPath path, VersionInfo versionInfo) => new(id, driveId, parentId, path, versionInfo);

    /// <summary>Creates a <see cref="DeletedDeltaItem"/>.</summary>
    public static DeletedDeltaItem CreateDeleted(OneDriveItemId id, DriveId driveId, OneDriveFolderId? parentId, ItemPath path) => new(id, driveId, parentId, path);
}
