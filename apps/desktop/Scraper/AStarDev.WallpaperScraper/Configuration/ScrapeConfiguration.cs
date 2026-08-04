namespace AStarDev.WallpaperScraper.Configuration;

/// <summary>
///     Mutable to support direct <see cref="IConfiguration" /> binding; not a candidate for the Records rule.
/// </summary>
public class ScrapeConfiguration
{
    /// <summary>
    ///    The name of the configuration section in the application configuration that corresponds to this class.
    /// </summary>
    public const string SectionName = "ScrapeConfiguration";

    /// <summary>
    ///     Gets or sets the display name of the application.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the database connection strings used by the application.
    /// </summary>
    public ConnectionStrings ConnectionStrings { get; set; } = new ConnectionStrings();

    /// <summary>
    ///     Gets or sets the directory used to persist the Playwright browser profile between sessions.
    /// </summary>
    public string UserDataDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "astardev-wallpaperscraper", "playwright-profile");

    /// <summary>
    ///     Gets or sets the search parameters guiding the scraper's navigation of the target website.
    /// </summary>
    public SearchConfiguration SearchConfiguration { get; set; } = new SearchConfiguration();

    public WindowSize WindowSize { get; set; } = new WindowSize(1_000, 1_000);
}
