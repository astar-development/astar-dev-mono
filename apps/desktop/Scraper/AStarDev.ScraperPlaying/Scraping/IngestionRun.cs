using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Everything that stays the same while one scrape pages through its results.</summary>
/// <param name="Request">The scrape being run.</param>
/// <param name="Context">The per-scrape ingestion state, including the HTTP client.</param>
/// <param name="Progress">Receives status messages.</param>
/// <param name="CancellationToken">Cancels the scrape.</param>
public sealed record IngestionRun(PageScrapeRequest Request, WallpaperIngestionContext Context, IProgress<string> Progress, CancellationToken CancellationToken)
{
    /// <summary>Gets the running count of the wallpapers this scrape has downloaded.</summary>
    public ScrapeTally Tally { get; } = new();
}
