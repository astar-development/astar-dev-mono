namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Remote versioning tags for a drive item, used for change detection.</summary>
public sealed record VersionInfo(string? ETag, string? CTag);
