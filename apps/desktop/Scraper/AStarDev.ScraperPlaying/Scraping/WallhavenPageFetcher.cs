using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using Microsoft.Extensions.Logging;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class WallhavenPageFetcher(IJsonResponseProcessor jsonResponseProcessor, ILogger<WallhavenPageFetcher> logger) : IWallhavenPageFetcher
{
    /// <inheritdoc/>
    public async Task<SearchResponse> FetchPageAsync(PageFetchRequest request, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var pageName = $"{request.LogLabel} page {request.Page}";
        progress.Report($"Fetching {pageName}.");
        LogMessage.PageView(logger, pageName);

        return (await jsonResponseProcessor.GetFromJsonAsync<SearchResponse>(request.PageUrl, client, cancellationToken))
            .Map(option => option.Match(value => value, () => throw new InvalidOperationException($"No response body received for {request.PageUrl}.")))
            .GetOrThrow();
    }
}
