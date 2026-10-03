using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>Fetches one page of Wallhaven search results.</summary>
public interface IWallhavenPageFetcher
{
    /// <summary>Fetches a page of search results, reporting that it is doing so.</summary>
    /// <param name="request">The page to fetch: its search label, URL and page number.</param>
    /// <param name="client">The client to fetch with.</param>
    /// <param name="progress">The progress reporter to report the fetch.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">The response had no body.</exception>
    Task<SearchResponse> FetchPageAsync(PageFetchRequest request, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken);
}
