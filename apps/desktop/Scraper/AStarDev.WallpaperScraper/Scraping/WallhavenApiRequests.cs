namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>What distinguishes a Wallhaven API request from any other request the scraper makes (such as an image download from the CDN).</summary>
internal static class WallhavenApiRequests
{
    /// <summary>The header that carries the user's API key.</summary>
    public const string ApiKeyHeaderName = "X-API-Key";

    private const string ApiPathPrefix = "/api/";

    /// <summary>Whether <paramref name="request"/> is aimed at the Wallhaven API.</summary>
    /// <param name="request">The request to inspect.</param>
    public static bool IsApiRequest(HttpRequestMessage request)
        => request.RequestUri?.AbsolutePath.StartsWith(ApiPathPrefix, StringComparison.Ordinal) == true;
}
