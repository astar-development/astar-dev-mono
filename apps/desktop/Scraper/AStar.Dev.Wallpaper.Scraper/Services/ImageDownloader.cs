using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Support;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public sealed class ImageDownloader(IImageRetriever imageRetriever, IDelayStrategy delayStrategy)
{
    public Task<Result<byte[], ScrapeError>> DownloadAsync(string imageUrl, CancellationToken cancellationToken)
        => RetryExtensions.RetryOnceAsync(
            () => imageRetriever.GetImageAsync(imageUrl, cancellationToken),
            () => delayStrategy.DelayAsync(DelayKind.Retry, cancellationToken));
}
