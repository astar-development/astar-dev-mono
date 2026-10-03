using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.UI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.WallpaperScraper.TestsUnit.UI;

/// <summary>Builds <see cref="StatusReporter"/> instances on a frozen clock so the timestamp on every message is predictable.</summary>
internal static class TestStatusReporter
{
    /// <summary>The <c>HH:mm:ss</c> timestamp every message appended by a reporter from <see cref="Create"/> carries.</summary>
    public const string Timestamp = "03:04:05";

    public static StatusReporter Create(IRateLimitBackoffNotifier? notifier = null)
        => new(NullLogger<StatusReporter>.Instance, new FrozenTimeProvider(), notifier ?? new RateLimitBackoffNotifier());

    private sealed class FrozenTimeProvider : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    }
}
