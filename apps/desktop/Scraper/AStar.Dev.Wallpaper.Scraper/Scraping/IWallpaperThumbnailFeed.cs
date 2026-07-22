namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Exposes the stream of wallpaper thumbnails published during a scrape.
/// </summary>
public interface IWallpaperThumbnailFeed
{
    /// <summary>
    ///     Gets the stream of thumbnail payloads, one per downloaded wallpaper.
    /// </summary>
    IObservable<WallpaperThumbnailPayload> Thumbnails { get; }

    /// <summary>
    ///     Gets the stream of category names skipped because they are already fully downloaded.
    /// </summary>
    IObservable<string> CategorySkipped { get; }
}
