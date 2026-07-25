using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

public sealed record DriveFolder(string Id, string Name, Option<string> ParentId);
