using System.IO.Abstractions;

namespace AStarDev.WallpaperScraper.Downloads;

/// <summary>Emptying of a directory's contents.</summary>
public static class DirectoryEmptier
{
    extension(IFileSystem fileSystem)
    {
        /// <summary>Deletes every file and subdirectory inside <paramref name="path"/>, keeping the directory itself. A blank, missing or file system root path is left alone.</summary>
        /// <param name="path">The directory to empty.</param>
        public void EmptyDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            var fullPath = fileSystem.Path.GetFullPath(path);
            if (fullPath == fileSystem.Path.GetPathRoot(fullPath) || !fileSystem.Directory.Exists(fullPath)) return;

            var directory = fileSystem.DirectoryInfo.New(fullPath);
            foreach (var file in directory.EnumerateFiles()) file.Delete();
            foreach (var subdirectory in directory.EnumerateDirectories()) subdirectory.Delete(true);
        }
    }
}
