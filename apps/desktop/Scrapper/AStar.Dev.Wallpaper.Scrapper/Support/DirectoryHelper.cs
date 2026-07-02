using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

internal static class DirectoryHelper
{
    public static DirectoryName CreateDirectoryIfRequired(List<string> fullDirectoryPath)
    {
        string directory = Path.Combine([.. fullDirectoryPath])!;
        directory = directory.CleanPath();

        //if(directory.LastIndexOf(':') > 2) fullDirectoryPath = directory[..2] + directory[2..].Replace(":", "_");

        if (Directory.Exists(directory)) return new(directory);

        _ = Directory.CreateDirectory(directory);

        return new(directory);
    }
}
