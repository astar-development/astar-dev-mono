using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using Avalonia.Controls;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Edits the search term and maximum results of a scrape configuration.</summary>
public sealed partial class SearchSettingsEditor : UserControl
{
    public SearchSettingsEditor() => InitializeComponent();

    /// <summary>Fills the controls from the specified settings.</summary>
    /// <param name="input">The settings to display.</param>
    public void Load(SearchSettingsInput input)
    {
        SearchTermBox.Text = input.SearchTerm;
        MaxResultsBox.Value = input.MaxResults.Match(maxResults => (decimal?)maxResults, () => null);
    }

    /// <summary>Reads the settings currently entered in the controls.</summary>
    public SearchSettingsInput ReadInput() => new(
        SearchTermBox.Text ?? string.Empty,
        MaxResultsBox.Value is { } maxResults ? Option.Some((int)maxResults) : Option.None<int>());
}
