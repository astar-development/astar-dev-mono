namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Checks the configured root directory the scraper saves wallpapers under.</summary>
public interface IRootDirectoryCheck
{
    /// <summary>Checks whether the configured root directory exists on disk.</summary>
    /// <returns><c>true</c> if the root directory exists; otherwise <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">No scrape configuration exists.</exception>
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);
}
