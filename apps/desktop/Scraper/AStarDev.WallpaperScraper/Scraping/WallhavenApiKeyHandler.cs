namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>
/// Keeps the user's API key on API requests only. The key is set as a default header on the Wallhaven client, so it would otherwise also
/// be sent with image downloads to the CDN host; this handler removes it from every request that is not an API request.
/// </summary>
public sealed class WallhavenApiKeyHandler : DelegatingHandler
{
    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!WallhavenApiRequests.IsApiRequest(request)) _ = request.Headers.Remove(WallhavenApiRequests.ApiKeyHeaderName);

        return base.SendAsync(request, cancellationToken);
    }
}
