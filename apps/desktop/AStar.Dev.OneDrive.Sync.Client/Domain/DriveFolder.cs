namespace AStar.Dev.OneDrive.Sync.Client.Domain;

public sealed record DriveFolder(string Id, string Name, string? ParentId = null);
