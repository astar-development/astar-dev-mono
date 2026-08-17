using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Factory for <see cref="DeltaItem"/> and its derived types.</summary>
public static class DeltaItemFactory
{
    /// <summary>Creates a <see cref="FileDeltaItem"/>.</summary>
    public static FileDeltaItem CreateFile(OneDriveItemId id, DriveId driveId, Option<OneDriveFolderId> parentId, ItemPath path, long size, Option<DateTimeOffset> lastModified, Option<string> downloadUrl, VersionInfo versionInfo) => new(id, driveId, parentId, path, size, lastModified, downloadUrl, versionInfo);

    /// <summary>Creates a <see cref="FolderDeltaItem"/>.</summary>
    public static FolderDeltaItem CreateFolder(OneDriveItemId id, DriveId driveId, Option<OneDriveFolderId> parentId, ItemPath path, VersionInfo versionInfo) => new(id, driveId, parentId, path, versionInfo);

    /// <summary>Creates a <see cref="DeletedDeltaItem"/>.</summary>
    public static DeletedDeltaItem CreateDeleted(OneDriveItemId id, DriveId driveId, Option<OneDriveFolderId> parentId, ItemPath path) => new(id, driveId, parentId, path);
}
