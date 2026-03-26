namespace AStar.Dev.OneDriveSync.old.Services;

/// <summary>AM-03: Represents a OneDrive folder returned by the Graph API.</summary>
public sealed record OneDriveFolder(string Id, string Name, bool HasChildren);
