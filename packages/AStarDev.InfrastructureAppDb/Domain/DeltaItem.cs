using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Represents a single item returned from the Microsoft Graph drive API.</summary>
public abstract record DeltaItem(OneDriveItemId Id, DriveId DriveId, Option<OneDriveFolderId> ParentId, ItemPath Path);

/// <summary>Represents a file item returned from the Microsoft Graph drive API.</summary>
public sealed record FileDeltaItem(OneDriveItemId Id, DriveId DriveId, Option<OneDriveFolderId> ParentId, ItemPath Path, long Size, Option<DateTimeOffset> LastModified, Option<string> DownloadUrl, VersionInfo VersionInfo) : DeltaItem(Id, DriveId, ParentId, Path);

/// <summary>Represents a folder item returned from the Microsoft Graph drive API.</summary>
public sealed record FolderDeltaItem(OneDriveItemId Id, DriveId DriveId, Option<OneDriveFolderId> ParentId, ItemPath Path, VersionInfo VersionInfo) : DeltaItem(Id, DriveId, ParentId, Path);

/// <summary>Represents an item that has been deleted remotely, as reported by the Microsoft Graph delta API.</summary>
public sealed record DeletedDeltaItem(OneDriveItemId Id, DriveId DriveId, Option<OneDriveFolderId> ParentId, ItemPath Path) : DeltaItem(Id, DriveId, ParentId, Path);
