using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

public sealed record DriveFolder(string Id, string Name, Option<string> ParentId);
