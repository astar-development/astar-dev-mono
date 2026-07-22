namespace AStar.Dev.Wallpaper.Scraper.Configuration;

/// <summary>
/// The <see cref="ApplicationMetadata" /> class provides metadata about the application, including its name, folder structure, and log file naming conventions.
/// </summary>
public static class ApplicationMetadata
{
    /// <summary>
    /// Gets the name of the application, used for display and identification purposes.
    /// </summary>
    public const string ApplicationName = "AStar.Dev.Wallpaper.Scraper";

    /// <summary>
    /// Gets the folder name used for storing application data, typically located in the user's application data directory.
    /// </summary>
    public const string ApplicationFolder = "astar.dev.wallpaper.scraper";

    /// <summary>
    /// Gets the hyphenated version of the application name, used for file naming and logging purposes.
    /// </summary>
    public const string ApplicationNameHyphenated = "astar-dev-wallpaper-scraper";

    /// <summary>
    /// Gets the dotted version of the application name, used for configuration and logging purposes.
    /// </summary>
    public const string ApplicationNameDotted = "astar.dev.wallpaper.scraper";

    /// <summary>
    /// Gets the name of the log file used by the application, typically located in the application's log directory.
    /// </summary>
    public const string ApplicationLogName = "astar-dev-wallpaper-scraper.log";
}
