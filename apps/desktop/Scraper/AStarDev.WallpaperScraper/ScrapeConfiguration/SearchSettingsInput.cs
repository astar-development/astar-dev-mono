using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>The unvalidated search settings as entered in the configuration editor.</summary>
/// <param name="SearchTerm">The entered search term.</param>
/// <param name="MaxResults">The entered maximum number of results, when limited.</param>
public sealed record SearchSettingsInput(string SearchTerm, Option<int> MaxResults)
{
    /// <summary>Creates the editor input from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static SearchSettingsInput From(ScrapeConfigurationEntity entity)
    {
        var settings = SearchSettings.From(entity);

        return new SearchSettingsInput(settings.SearchTerm, settings.MaxResults);
    }

    /// <summary>Validates the input: the search term may be blank and is trimmed; a maximum number of results, when set, must be greater than zero.</summary>
    /// <returns>The validated <see cref="SearchSettings"/>, or every validation error found.</returns>
    public Validation<SearchSettings> Validate() =>
        MaxResults.Match(maxResults => maxResults > 0, () => true)
            ? Validation.Valid(new SearchSettings(SearchTerm.Trim(), MaxResults))
            : Validation.Invalid<SearchSettings>(ValidationErrorFactory.Create(nameof(MaxResults), "Must be greater than zero."));
}
