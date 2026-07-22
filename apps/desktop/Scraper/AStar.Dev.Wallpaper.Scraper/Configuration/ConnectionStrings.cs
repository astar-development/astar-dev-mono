namespace AStar.Dev.Wallpaper.Scraper.Configuration;

/// <summary>
///     Mutable to support direct <see cref="IConfiguration" /> binding; not a candidate for the Records rule.
/// </summary>
public class ConnectionStrings
{
    /// <summary>
    ///     Gets or sets the SQLite connection string used to store scraped data.
    /// </summary>
    public string Sqlite { get; set; } = string.Empty;
}
