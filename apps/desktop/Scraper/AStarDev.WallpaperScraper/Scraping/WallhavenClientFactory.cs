namespace AStarDev.WallpaperScraper.Scraping;

/// <inheritdoc/>
public sealed class WallhavenClientFactory(IHttpClientFactory httpClientFactory) : IWallhavenClientFactory
{
    /// <inheritdoc/>
    public HttpClient Create(WallhavenConnection connection)
    {
        var httpClient = httpClientFactory.CreateClient(ApplicationConstants.WallhavenHttpClientName);
        httpClient.BaseAddress = connection.BaseUrl;
        httpClient.DefaultRequestHeaders.Add(WallhavenApiRequests.ApiKeyHeaderName, connection.ApiKey);
        httpClient.DefaultRequestHeaders.Referrer = connection.BaseUrl;

        return httpClient;
    }
}
