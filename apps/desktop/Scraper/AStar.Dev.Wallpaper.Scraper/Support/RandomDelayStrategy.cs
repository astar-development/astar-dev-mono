using AStar.Dev.Wallpaper.Scraper.Models;

namespace AStar.Dev.Wallpaper.Scraper.Support;

/// <summary>The production <see cref="IDelayStrategy" />, using <see cref="Random" /> and <see cref="Task.Delay(TimeSpan,CancellationToken)" />.</summary>
public sealed class RandomDelayStrategy(ScrapeConfiguration scrapeConfiguration) : IDelayStrategy
{
    /// <inheritdoc />
    public Task DelayAsync(DelayKind delayKind, CancellationToken cancellationToken = default)
        => delayKind switch
        {
            DelayKind.CategoryUpToDate => Task.Delay(TimeSpan.FromSeconds(new Random().Next(1, 5)), cancellationToken),
            DelayKind.PageNavigation => Task.Delay(TimeSpan.FromSeconds(RandomImagePauseSeconds()), cancellationToken),
            DelayKind.ImageAlreadyDownloaded => Task.Delay(ScraperConstants.ImageAlreadyDownloadedDelay, cancellationToken),
            DelayKind.BeforeImage => Task.Delay(TimeSpan.FromSeconds(RandomImagePauseSeconds()), cancellationToken),
            DelayKind.Retry => Task.Delay(ScraperConstants.RetryDelay, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(delayKind), delayKind, "Unrecognised delay kind."),
        };

    private int RandomImagePauseSeconds()
        => Random.Shared.Next(scrapeConfiguration.SearchConfiguration.ImagePauseInSeconds, scrapeConfiguration.SearchConfiguration.ImagePauseInSeconds + 4);
}
