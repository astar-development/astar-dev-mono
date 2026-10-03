using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>The validated search categories of a scrape configuration.</summary>
/// <param name="Categories">The complete list of categories the configuration should have after saving.</param>
public sealed record SearchCategoriesSettings(IReadOnlyList<SearchCategorySettings> Categories) : IScrapeConfigurationSectionEdit
{
    /// <inheritdoc/>
    /// <remarks>Categories are matched to existing ones by id, ignoring case. Matches keep their key and scrape progress, unmatched existing categories are removed and unmatched settings are added.</remarks>
    public void ApplyTo(ScrapeConfigurationEntity entity, DateTimeOffset now)
    {
        var search = entity.SearchConfiguration;
        var keptIds = Categories.Select(category => category.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var removed in search.SearchCategories.Where(existing => !keptIds.Contains(existing.Id)).ToList())
        {
            _ = search.SearchCategories.Remove(removed);
        }

        var existingById = new Dictionary<string, SearchCategoryEntity>(StringComparer.OrdinalIgnoreCase);
        foreach (var existing in search.SearchCategories)
        {
            existingById[existing.Id] = existing;
        }

        foreach (var category in Categories)
        {
            if (existingById.TryGetValue(category.Id, out var existing)) UpdateCategory(existing, category, now);
            else existingById[category.Id] = AddCategory(search, category);
        }
    }

    private static SearchCategoryEntity AddCategory(SearchConfigurationEntity search, SearchCategorySettings category)
    {
        var added = new SearchCategoryEntity
        {
            SearchConfigurationId = search.Id,
            Id = category.Id,
            Name = category.Name,
            IncludeInSearch = category.IncludeInSearch,
            IsFamous = category.IsFamous,
            IsInternet = category.IsInternet
        };
        search.SearchCategories.Add(added);

        return added;
    }

    private static void UpdateCategory(SearchCategoryEntity existing, SearchCategorySettings category, DateTimeOffset now)
    {
        existing.Name = category.Name;
        existing.IncludeInSearch = category.IncludeInSearch;
        existing.IsFamous = category.IsFamous;
        existing.IsInternet = category.IsInternet;
        existing.UpdatedAt = now;
    }
}
