namespace AStar.Dev.OneDrive.Sync.Client.Models;

public sealed record DeltaItem(
    string Id,
    string DriveId,
    string Name,
    string? ParentId,
    bool IsFolder,
    bool IsDeleted,
    long Size,
    DateTimeOffset? LastModified,
    string? DownloadUrl,
    string? RelativePath = null);
