using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>The editable state of one search category row in <see cref="SearchCategoriesEditor"/>.</summary>
/// <param name="input">The category input to display.</param>
public sealed class SearchCategoryRow(SearchCategoryInput input)
{
    /// <summary>The category identifier. Read-only in the editor for an existing category because it is the database key.</summary>
    public string Id { get; set; } = input.Id;

    /// <summary>The category name.</summary>
    public string Name { get; set; } = input.Name;

    /// <summary>Whether the category is included in the scraping process.</summary>
    public bool IncludeInSearch { get; set; } = input.IncludeInSearch;

    /// <summary>Whether the category defines a famous person.</summary>
    public bool IsFamous { get; set; } = input.IsFamous;

    /// <summary>Whether the category defines the internet classification.</summary>
    public bool IsInternet { get; set; } = input.IsInternet;

    /// <summary>Whether the category already exists in the database.</summary>
    public bool IsExisting { get; } = input.IsExisting;

    /// <summary>The read-only scrape progress summary.</summary>
    public string ProgressText { get; } = input.Progress.Match(
        value => $"Images: {value.LastKnownImageCount}, page {value.LastPageVisited} of {value.TotalPages}",
        () => "New category");

    /// <summary>Creates the input represented by the current row values.</summary>
    public SearchCategoryInput ToInput() => new(Id ?? string.Empty, Name ?? string.Empty, IncludeInSearch, IsFamous, IsInternet, input.Progress);
}
