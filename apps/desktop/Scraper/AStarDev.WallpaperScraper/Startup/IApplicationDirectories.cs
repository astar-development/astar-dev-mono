namespace AStarDev.WallpaperScraper.Startup;

/// <summary>
/// The <see cref="ApplicationDirectories" /> class provides access to the application's data, cache, and log directories, ensuring they exist and are accessible.
/// </summary>
public interface IApplicationDirectories
{
    /// <summary>
    /// Ensures that the necessary application directories exist, creating them if they do not.
    /// </summary>
    void CreateIfRequired();
}
