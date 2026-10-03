using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>
/// Interface for ingesting a page of wallpapers: skipping those whose file record already exists (found with a
/// single query for the whole page), and for the rest downloading the image, persisting the file record, and
/// fetching/linking the tags.
/// </summary>
public interface IWallpaperIngestionService
{
    /// <summary>
    /// Ingests a page of wallpapers into the run's directory, reporting progress and any
    /// failure at each step without throwing - a failure at any step is reported and that wallpaper is skipped,
    /// allowing the caller to continue processing the rest of the page.
    /// </summary>
    /// <param name="wallpapers">The wallpaper data to ingest.</param>
    /// <param name="run">The scrape being run: supplies the per-page state (save directory, HTTP client, file repository) to ingest the wallpapers into, the progress reporter and the cancellation token.</param>
    /// <returns><see cref="IngestOutcome.Incomplete"/> if any wallpaper on the page was skipped because a step failed (so the page must be visited again), otherwise <see cref="IngestOutcome.Complete"/>.</returns>
    Task<IngestOutcome> IngestPageAsync(IReadOnlyList<Data> wallpapers, IngestionRun run);
}
