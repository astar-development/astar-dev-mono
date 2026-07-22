namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Writes a downloaded wallpaper image to disk.
/// </summary>
public interface IWallpaperFileStore
{
    /// <summary>
    ///     Saves the image bytes under the given directory, creating the directory if needed.
    /// </summary>
    /// <param name="directoryPath">The directory to save the image under.</param>
    /// <param name="fileName">The file name to save the image as.</param>
    /// <param name="imageBytes">The image bytes to write.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the save.</param>
    Task<SavedWallpaperFile> SaveAsync(string directoryPath, string fileName, byte[] imageBytes, CancellationToken cancellationToken);
}
