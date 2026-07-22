namespace AStar.Dev.Wallpaper.Scraper.Configuration;

/// <summary>
///     The search parameters guiding the scraper's navigation of the target website.
///     Mutable to support direct <see cref="IConfiguration" /> binding; not a candidate for the Records rule.
/// </summary>
public class SearchConfiguration
{
    /// <summary>
    ///     Gets or sets the base URL of the target website.
    /// </summary>
    public Uri BaseUrl { get; set; } = new("https://example.com");

    /// <summary>
    ///     Gets or sets a value indicating whether the browser runs in headless mode.
    /// </summary>
    public bool UseHeadless { get; set; }
}
