using AStar.Dev.FunctionalParadigm;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Decorates <see cref="IWallpaperImageDownloader" /> so that a thumbnail is generated and published for every
///     downloaded wallpaper, without the downloader itself needing to know about thumbnails.
/// </summary>
public sealed class ThumbnailPublishingWallpaperImageDownloader(IRawWallpaperImageDownloader inner, IWallpaperThumbnailGenerator thumbnailGenerator, IWallpaperThumbnailPublisher thumbnailPublisher) : IWallpaperImageDownloader
{
    /// <inheritdoc />
    public Task<Exceptional<byte[]>> DownloadAsync(IPage page, string imageUrl, string categoryName, IReadOnlyList<string> tags, CancellationToken cancellationToken) =>
        inner.DownloadAsync(page, imageUrl, categoryName, tags, cancellationToken)
            .TapAsync(imageBytes => thumbnailPublisher.Publish(WallpaperThumbnailPayloadFactory.Create(thumbnailGenerator.Generate(imageBytes), categoryName, tags)));
}
