using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>One unvalidated search category row as entered in the configuration editor.</summary>
/// <param name="Id">The entered category identifier.</param>
/// <param name="Name">The entered category name.</param>
/// <param name="IncludeInSearch">Whether the category is included in the scraping process.</param>
/// <param name="IsFamous">Whether the category defines a famous person.</param>
/// <param name="IsInternet">Whether the category defines the internet classification.</param>
/// <param name="Progress">The scrape progress when the category already exists, none for a category added in the editor.</param>
public sealed record SearchCategoryInput(string Id, string Name, bool IncludeInSearch, bool IsFamous, bool IsInternet, Option<SearchCategoryProgress> Progress)
{
    /// <summary>Whether the category already exists in the database, so its id is its key and must not change.</summary>
    public bool IsExisting => Progress.Match(_ => true, () => false);
}

/// <summary>The unvalidated search categories as entered in the configuration editor.</summary>
/// <param name="Categories">The category rows, in display order.</param>
public sealed record SearchCategoriesInput(IReadOnlyList<SearchCategoryInput> Categories)
{
    /// <summary>Creates the editor input from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static SearchCategoriesInput From(ScrapeConfigurationEntity entity) =>
        new([.. entity.SearchConfiguration.SearchCategories.Select(category => new SearchCategoryInput(
            category.Id,
            category.Name,
            category.IncludeInSearch,
            category.IsFamous,
            category.IsInternet,
            Option.Some(new SearchCategoryProgress(category.LastKnownImageCount, category.LastPageVisited, category.TotalPages))))]);

    /// <summary>Validates the input: every id and name must be non-blank and ids must be unique ignoring case. Text is trimmed.</summary>
    /// <returns>The validated <see cref="SearchCategoriesSettings"/>, or every validation error found.</returns>
    public Validation<SearchCategoriesSettings> Validate()
    {
        List<ValidationError> errors = [];
        HashSet<string> seenIds = new(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Categories.Count; i++)
        {
            var category = Categories[i];
            var id = category.Id.Trim();
            if (id.Length == 0) errors.Add(ValidationErrorFactory.Create($"SearchCategories[{i}].Id", "Must not be blank."));
            else if (!seenIds.Add(id)) errors.Add(ValidationErrorFactory.Create($"SearchCategories[{i}].Id", $"Duplicate id '{id}'."));

            if (string.IsNullOrWhiteSpace(category.Name)) errors.Add(ValidationErrorFactory.Create($"SearchCategories[{i}].Name", "Must not be blank."));
        }

        return errors.Count > 0
            ? Validation.Invalid<SearchCategoriesSettings>(errors)
            : Validation.Valid(new SearchCategoriesSettings([.. Categories.Select(category => new SearchCategorySettings(category.Id.Trim(), category.Name.Trim(), category.IncludeInSearch, category.IsFamous, category.IsInternet))]));
    }
}
