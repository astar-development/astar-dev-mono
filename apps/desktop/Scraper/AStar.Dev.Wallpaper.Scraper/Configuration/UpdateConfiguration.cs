namespace AStar.Dev.Wallpaper.Scraper.Configuration;

/// <summary>
///     Mutable to support direct <see cref="IConfiguration" /> binding; not a candidate for the Records rule.
/// </summary>
public class UpdateConfiguration
{
    /// <summary>
    ///     Gets or sets the GitHub repository URL that the update service polls for release information.
    /// </summary>
    public string RepositoryUrl { get; set; } = string.Empty;
}
