using AStarDev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>Interface for resolving the directory a wallpaper's image and file record should be saved under.</summary>
public interface ISaveDirectoryResolver
{
    /// <summary>
    /// Resolves the directories a wallpaper's image and file record can be saved under: the configured scrape root directory, and the configured famous root directory,
    /// each combined with either "top-wallpapers" or the slugified search category name.
    /// </summary>
    /// <param name="categoryName">The search category name when a category search is being performed, or <see cref="Option{T}.None"/> when processing the "Top Wallpapers" scrape.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the resolved directories.</returns>
    Task<SaveDirectories> ResolveSaveDirectoriesAsync(Option<string> categoryName, CancellationToken cancellationToken);
}
