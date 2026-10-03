using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>The validated search settings of a scrape configuration.</summary>
/// <param name="SearchTerm">The search term used for scraping wallpapers.</param>
/// <param name="MaxResults">The maximum number of results to retrieve, when limited.</param>
public sealed record SearchSettings(string SearchTerm, Option<int> MaxResults) : IScrapeConfigurationSectionEdit
{
    /// <summary>Copies the search settings from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static SearchSettings From(ScrapeConfigurationEntity entity) => new(
        entity.SearchConfiguration.SearchTerm,
        entity.SearchConfiguration.MaxResults is { } maxResults ? Option.Some(maxResults) : Option.None<int>());

    /// <inheritdoc/>
    public void ApplyTo(ScrapeConfigurationEntity entity, DateTimeOffset now)
    {
        entity.SearchConfiguration.SearchTerm = SearchTerm;
        entity.SearchConfiguration.MaxResults = MaxResults.Match(maxResults => (int?)maxResults, () => null);
        entity.SearchConfiguration.UpdatedAt = now;
    }
}
