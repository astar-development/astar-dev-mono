namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>
/// Interface for publishing that the scraper is backing off after the Wallhaven API rate limited it, so interested parties (such as
/// the status display) can tell the user without the HTTP pipeline needing to know about them.
/// </summary>
public interface IRateLimitBackoffNotifier
{
    /// <summary>Raised when a request has been rate limited and is about to wait before retrying; the argument is how long the wait is.</summary>
    event EventHandler<TimeSpan>? BackingOff;

    /// <summary>Raises <see cref="BackingOff"/> for a wait of <paramref name="delay"/>.</summary>
    /// <param name="delay">How long the scraper is about to wait before retrying.</param>
    void NotifyBackingOff(TimeSpan delay);
}
