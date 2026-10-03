using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>
/// Ingests one wallpaper known not to have a file record yet, in two steps so a caller can fetch the next wallpaper's tags while this one downloads:
/// <see cref="FetchTagsAsync"/> only talks to the network (safe to run alongside <see cref="IngestAsync"/>), while <see cref="IngestAsync"/> downloads the image
/// and uses the database, so it must not be run for two wallpapers at once.
/// </summary>
public interface INewWallpaperIngestor
{
    /// <summary>Fetches the wallpaper's tags. A failure to fetch them is reported and gives none rather than throwing, so the caller can leave the wallpaper to be retried on a later scrape; a cancellation is not swallowed.</summary>
    /// <param name="wallpaper">The wallpaper whose tags to fetch.</param>
    /// <param name="run">The scrape being run: supplies the HTTP client, the progress reporter and the cancellation token.</param>
    /// <returns>The wallpaper's tags, or <see cref="Option{T}.None"/> if they could not be fetched.</returns>
    Task<Option<IReadOnlyList<Tag>>> FetchTagsAsync(Data wallpaper, IngestionRun run);

    /// <summary>
    /// Downloads the wallpaper into the run's directory, records it and links <paramref name="tags"/> (or, if any tag is flagged to ignore images, does nothing), reporting a failure at any step without throwing so the
    /// caller can continue with the rest of the page. If tags cannot be linked the recorded file is discarded rather than left untagged. A cancellation is not swallowed (a file recorded but not yet tagged is discarded first).
    /// </summary>
    /// <param name="candidate">The wallpaper data to ingest, and the file extension to save the image under.</param>
    /// <param name="tags">The wallpaper's tags, from <see cref="FetchTagsAsync"/>.</param>
    /// <param name="run">The scrape being run: supplies the per-page state to ingest the wallpaper into, the progress reporter and the cancellation token.</param>
    /// <returns><see cref="IngestOutcome.Incomplete"/> if a step failed, in which case nothing is left recorded for the wallpaper so a later scrape retries it; otherwise <see cref="IngestOutcome.Complete"/>.</returns>
    Task<IngestOutcome> IngestAsync(WallpaperCandidate candidate, IReadOnlyList<Tag> tags, IngestionRun run);
}
