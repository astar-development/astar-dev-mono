using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>
/// Provides helper methods for working with directories, including creating directories if they do not exist.
/// </summary>
public interface IDirectoryHelper
{
    /// <summary>
    /// Creates a directory at the specified path if it does not already exist. Returns a DirectoryName record containing the path of the created or existing directory.
    /// </summary>
    /// <param name="fullDirectoryPath">The full path of the directory to create.</param>
    /// <returns>The path of the created or existing directory as a strongly-typed DirectoryName instance.</returns>
    DirectoryName CreateDirectoryIfRequired(List<string> fullDirectoryPath);
}
