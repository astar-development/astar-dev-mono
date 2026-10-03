using System.Diagnostics.CodeAnalysis;

namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <summary>The range of the random wait applied before each image download, so the scraper does not hammer the image host.</summary>
/// <param name="Minimum">The shortest wait.</param>
/// <param name="Maximum">The longest wait.</param>
public sealed record DownloadPacing(TimeSpan Minimum, TimeSpan Maximum)
{
    /// <summary>The production pacing: a random wait of two to three seconds.</summary>
    public static DownloadPacing Default { get; } = new(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));

    /// <summary>No wait, for tests and callers that must not be slowed.</summary>
    public static DownloadPacing None { get; } = new(TimeSpan.Zero, TimeSpan.Zero);

    /// <summary>Picks a random wait between <see cref="Minimum"/> and <see cref="Maximum"/>.</summary>
    /// <returns>The wait to apply before the next download.</returns>
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "The value only spreads out requests; it has no security purpose.")]
    public TimeSpan NextDelay() => Minimum + ((Maximum - Minimum) * Random.Shared.NextDouble());
}
