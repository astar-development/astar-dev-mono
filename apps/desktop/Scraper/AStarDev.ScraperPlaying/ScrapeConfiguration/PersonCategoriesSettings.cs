using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>The validated editable values of one person category.</summary>
/// <param name="Id">The identifier of the existing category, none for a category added in the editor.</param>
/// <param name="Name">The Wallhaven tag category name.</param>
public sealed record PersonCategorySettings(Option<Guid> Id, string Name);

/// <summary>The validated person categories of a scrape configuration.</summary>
/// <param name="Categories">The complete list of person categories the configuration should have after saving.</param>
public sealed record PersonCategoriesSettings(IReadOnlyList<PersonCategorySettings> Categories) : IScrapeConfigurationSectionEdit
{
    /// <inheritdoc/>
    /// <remarks>Categories are matched to existing ones by id, so a rename keeps the same row. Unmatched existing categories are removed and unmatched settings are added.</remarks>
    public void ApplyTo(ScrapeConfigurationEntity entity, DateTimeOffset now)
    {
        var search = entity.SearchConfiguration;
        var keptIds = Categories.Select(category => category.Id.Match(id => id, () => Guid.Empty)).ToHashSet();

        foreach (var removed in search.PersonCategories.Where(existing => !keptIds.Contains(existing.Id)).ToList())
        {
            _ = search.PersonCategories.Remove(removed);
        }

        var existingById = search.PersonCategories.ToDictionary(existing => existing.Id);

        foreach (var category in Categories)
        {
            var existing = category.Id.Match(id => existingById.GetValueOrDefault(id), () => null);
            if (existing is null)
            {
                // An empty id lets EF generate one and treat the row as added; a pre-set id would make EF update a row that does not exist.
                search.PersonCategories.Add(new PersonCategoryEntity { Id = Guid.Empty, SearchConfigurationId = search.Id, Name = category.Name });

                continue;
            }

            existing.Name = category.Name;
            existing.UpdatedAt = now;
        }
    }
}
