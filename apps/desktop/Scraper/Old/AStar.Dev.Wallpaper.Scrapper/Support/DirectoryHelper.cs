using System.IO.Abstractions;
using AStar.Dev.Guard.Clauses;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <inheritdoc/>
public sealed class DirectoryHelper(IFileSystem fileSystem) : IDirectoryHelper
{
    /// <inheritdoc/>
    public DirectoryName CreateDirectoryIfRequired(List<string> fullDirectoryPath)
    {
        GuardAgainst.Null(fullDirectoryPath);

        if (fullDirectoryPath.Count == 0) return new(string.Empty);

        string directory = fullDirectoryPath[0].CombinePath([.. fullDirectoryPath.Skip(1),]);

        _ = fileSystem.Directory.CreateDirectory(directory.CleanPath());

        return new(directory);
    }
}
