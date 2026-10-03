using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Creates the per-scrape ingestion state, including the HTTP client, for a <see cref="PageScrapeRequest"/>.</summary>
public interface IWallpaperIngestionContextFactory
{
    /// <summary>Creates the context; its client is used for both page fetches and wallpaper downloads.</summary>
    /// <param name="request">The scrape being started.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task<WallpaperIngestionContext> CreateAsync(PageScrapeRequest request, CancellationToken cancellationToken);
}
