using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Represents a folder on a OneDrive as returned by the Microsoft Graph API.</summary>
/// <param name="Id">The remote identifier of the folder.</param>
/// <param name="Name">The display name of the folder.</param>
/// <param name="ParentId">The remote identifier of the parent folder, when the folder is not the drive root.</param>
public sealed record DriveFolder(string Id, string Name, Option<string> ParentId);
