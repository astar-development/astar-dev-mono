namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class RateLimitBackoffNotifier : IRateLimitBackoffNotifier
{
    /// <inheritdoc/>
    public event EventHandler<TimeSpan>? BackingOff;

    /// <inheritdoc/>
    public void NotifyBackingOff(TimeSpan delay) => BackingOff?.Invoke(this, delay);
}
