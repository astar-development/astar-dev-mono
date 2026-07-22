namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Reads the one-shot configuration snapshot a scrape run needs, before any Playwright navigation begins.
/// </summary>
public interface IScrapeContextReader
{
    /// <summary>
    ///     Reads the current search categories, ignore lists, and directory layout from the database.
    /// </summary>
    /// <param name="cancellationToken">A token used to observe cancellation of the read.</param>
    Task<ScrapeContext> ReadAsync(CancellationToken cancellationToken);
}
