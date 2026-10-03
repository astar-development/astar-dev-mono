using Microsoft.Extensions.Configuration;

namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>How much a single scrape is allowed to fetch.</summary>
/// <param name="MaximumSearchCategories">The maximum number of configured search categories searched, before the top wallpapers search.</param>
/// <param name="MaximumPagesPerSearch">The maximum number of result pages fetched for each search.</param>
public sealed record ScrapeLimits(int MaximumSearchCategories, int MaximumPagesPerSearch)
{
    private const string SectionName = "ScrapeLimits";

    /// <summary>The limits used when none are configured: no limit, so every category and every page is scraped.</summary>
    public static ScrapeLimits Default { get; } = new(int.MaxValue, int.MaxValue);

    /// <summary>Reads the limits from the <c>ScrapeLimits</c> configuration section; a missing, unparsable or less-than-one value means that limit is unlimited.</summary>
    /// <param name="configuration">The application configuration.</param>
    public static ScrapeLimits From(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        return new ScrapeLimits(
            PositiveOrDefault(section[nameof(MaximumSearchCategories)], Default.MaximumSearchCategories),
            PositiveOrDefault(section[nameof(MaximumPagesPerSearch)], Default.MaximumPagesPerSearch));
    }

    private static int PositiveOrDefault(string? configuredValue, int defaultValue)
        => int.TryParse(configuredValue, out var parsed) && parsed > 0 ? parsed : defaultValue;
}
