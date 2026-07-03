using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

public sealed record DriveFolder(string Id, string Name, Option<string> ParentId);
