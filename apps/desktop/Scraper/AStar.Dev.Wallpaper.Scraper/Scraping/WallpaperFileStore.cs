using System.IO.Abstractions;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Writes a downloaded wallpaper image to disk.
/// </summary>
public sealed class WallpaperFileStore(IFileSystem fileSystem) : IWallpaperFileStore
{
    /// <inheritdoc />
    public Task<SavedWallpaperFile> SaveAsync(string directoryPath, string fileName, byte[] imageBytes, CancellationToken cancellationToken)
    {
        fileSystem.Directory.CreateDirectory(directoryPath);
        string fullPath = fileSystem.Path.Combine(directoryPath, fileName);
        fileSystem.File.WriteAllBytes(fullPath, imageBytes);

        return Task.FromResult(new SavedWallpaperFile(fullPath, fileSystem.FileInfo.New(fullPath).Length));
    }
}
