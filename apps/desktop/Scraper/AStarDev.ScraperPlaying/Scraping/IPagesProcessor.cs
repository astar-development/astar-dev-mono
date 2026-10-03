namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for processing pages by fetching and handling their content asynchronously.</summary>
public interface IPagesProcessor
{
    /// <summary>Fetches and processes pages asynchronously based on the provided request.</summary>
    /// <param name="request">Describes the scrape: label, category, previous progress, page URLs, connection and person categories.</param>
    /// <param name="progress">The progress reporter to report the fetching and processing progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <remarks>Progress is recorded through the request's <see cref="PageHooks.OnPageCompleted"/> hook, once per fully ingested page (a page with a failed wallpaper holds progress back, so it is retried). A search skipped because nothing has changed since the previous scrape records nothing.</remarks>
    Task FetchAndProcessPagesAsync(PageScrapeRequest request, IProgress<string> progress, CancellationToken cancellationToken);
}
