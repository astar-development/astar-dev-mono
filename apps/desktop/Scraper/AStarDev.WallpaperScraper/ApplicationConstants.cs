namespace AStarDev.WallpaperScraper;

/// <summary>Application-wide constants shared across multiple types.</summary>
public static class ApplicationConstants
{
    /// <summary>The named <see cref="HttpClient"/> configured with the scraper's static headers via <c>AddHttpClient</c>.</summary>
    public const string WallhavenHttpClientName = "Wallhaven";

    /// <summary>The relative path, appended to a wallpaper id, of Wallhaven's per-wallpaper detail endpoint.</summary>
    public const string WallhavenDetailPathTemplate = "api/v1/w/";

    /// <summary>
    /// The number of Wallhaven API requests permitted per <see cref="WallhavenRateLimitWindow"/>. Wallhaven's documented limit is 45 per
    /// minute; this stays below it so the scraper's own window boundaries never trip the server-side 429.
    /// </summary>
    public const int WallhavenRequestsPerWindow = 40;

    /// <summary>The window over which <see cref="WallhavenRequestsPerWindow"/> applies.</summary>
    public static readonly TimeSpan WallhavenRateLimitWindow = TimeSpan.FromMinutes(1);
}
