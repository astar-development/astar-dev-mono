namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Represents a single item returned by the Microsoft Graph delta API.</summary>
public sealed record DeltaItem(OneDriveItemId Id, string DriveId, OneDriveFolderId? ParentId, ItemPath Path, bool IsFolder, bool IsDeleted, long Size, DateTimeOffset? LastModified, string? DownloadUrl, VersionInfo VersionInfo);
