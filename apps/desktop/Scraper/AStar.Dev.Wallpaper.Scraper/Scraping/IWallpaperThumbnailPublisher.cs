namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Publishes newly generated wallpaper thumbnails as they become available during a scrape.
/// </summary>
public interface IWallpaperThumbnailPublisher
{
    /// <summary>
    ///     Publishes a thumbnail, along with the category and tags it was downloaded under, to any subscribers.
    /// </summary>
    /// <param name="payload">The thumbnail bytes and their associated category and tags.</param>
    void Publish(WallpaperThumbnailPayload payload);

    /// <summary>
    ///     Publishes the name of a category that was skipped because it is already fully downloaded.
    /// </summary>
    /// <param name="categoryName">The name of the category that was skipped.</param>
    void PublishCategorySkipped(string categoryName);
}
