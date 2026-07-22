namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Represents the directory naming conventions used to store downloaded wallpapers.
/// </summary>
/// <param name="RootDirectory">The root directory all wallpapers are stored under.</param>
/// <param name="BaseDirectory">The directory segment appended to the root for ordinary wallpapers.</param>
/// <param name="BaseDirectoryFamous">The directory segment appended to the root for wallpapers tagged as famous.</param>
public sealed record DirectoryLayout(string RootDirectory, string BaseDirectory, string BaseDirectoryFamous);
