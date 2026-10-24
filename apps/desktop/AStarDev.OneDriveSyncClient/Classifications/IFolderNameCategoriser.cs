using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <summary>Derives classifications from the folder names in a remote path, as an additional signal that never replaces the others.</summary>
public interface IFolderNameCategoriser
{
    /// <summary>Returns one classification for each distinct recognised folder name in <paramref name="remotePath"/> (the file name itself is ignored). Returns an empty list when none are recognised.</summary>
    IReadOnlyList<FileClassification> Categorise(string remotePath);
}
