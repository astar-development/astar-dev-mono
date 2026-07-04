using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <inheritdoc/>
public class DirectoryHelper : IDirectoryHelper
{
    /// <inheritdoc/>
    public DirectoryName CreateDirectoryIfRequired(List<string> fullDirectoryPath)
    {
        string directory = Path.Combine([.. fullDirectoryPath])!;

        _ = Directory.CreateDirectory(directory.CleanPath());

        return new(directory);
    }
}
