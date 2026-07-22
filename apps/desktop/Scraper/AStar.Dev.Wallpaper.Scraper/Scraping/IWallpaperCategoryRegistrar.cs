namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Ensures every tagged category observed while scraping exists in the file classification taxonomy.
/// </summary>
public interface IWallpaperCategoryRegistrar
{
    /// <summary>
    ///     Adds a classification category for each tag not already present, keyed by tag name.
    /// </summary>
    /// <param name="tags">The wallpaper's curated tags.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the write.</param>
    Task EnsureCategoriesExistAsync(IReadOnlyList<TagData> tags, CancellationToken cancellationToken);
}
