using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Home;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Represents a single item returned by the Microsoft Graph delta API.</summary>
public sealed record DeltaItem(OneDriveItemId Id, DriveId DriveId, OneDriveFolderId? ParentId, ItemPath Path, bool IsFolder, bool IsDeleted, long Size, DateTimeOffset? LastModified, string? DownloadUrl, VersionInfo VersionInfo);
