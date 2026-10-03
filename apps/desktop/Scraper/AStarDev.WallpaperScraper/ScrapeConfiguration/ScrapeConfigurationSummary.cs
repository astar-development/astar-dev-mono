using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>Identifies a root scrape configuration and the label shown for it in the configuration picker.</summary>
/// <param name="Id">The unique identifier of the scrape configuration.</param>
/// <param name="Label">The human-readable label describing the scrape configuration.</param>
public sealed record ScrapeConfigurationSummary(ScrapeConfigurationId Id, string Label)
{
    /// <summary>Creates a summary for the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to summarise.</param>
    /// <returns>The summary, labelled with the site host and, when set, the search term.</returns>
    public static ScrapeConfigurationSummary From(ScrapeConfigurationEntity entity)
        => From(new ScrapeConfigurationHeader(entity.Id, entity.BaseUrl, entity.SearchConfiguration.SearchTerm));

    /// <summary>Creates a summary for the specified scrape configuration header.</summary>
    /// <param name="header">The header to summarise.</param>
    /// <returns>The summary, labelled with the site host and, when set, the search term.</returns>
    public static ScrapeConfigurationSummary From(ScrapeConfigurationHeader header)
    {
        var site = header.BaseUrl.IsAbsoluteUri ? header.BaseUrl.Host : header.BaseUrl.OriginalString;

        return new ScrapeConfigurationSummary(header.Id, string.IsNullOrWhiteSpace(header.SearchTerm) ? site : $"{site} - {header.SearchTerm}");
    }

    /// <inheritdoc/>
    public override string ToString() => Label;
}
