using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>The editable state of one person category row in <see cref="PersonCategoriesEditor"/>.</summary>
/// <param name="input">The category input to display.</param>
public sealed class PersonCategoryRow(PersonCategoryInput input)
{
    /// <summary>The Wallhaven tag category name.</summary>
    public string Name { get; set; } = input.Name;

    /// <summary>Creates the input represented by the current row values.</summary>
    public PersonCategoryInput ToInput() => new(input.Id, Name ?? string.Empty);
}
