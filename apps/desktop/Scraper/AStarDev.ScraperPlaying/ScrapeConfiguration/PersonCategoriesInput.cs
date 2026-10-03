using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>One unvalidated person category row as entered in the configuration editor.</summary>
/// <param name="Id">The identifier of the existing category, none for a category added in the editor.</param>
/// <param name="Name">The entered Wallhaven tag category name.</param>
public sealed record PersonCategoryInput(Option<Guid> Id, string Name);

/// <summary>The unvalidated person categories as entered in the configuration editor.</summary>
/// <param name="Categories">The category rows, in display order.</param>
public sealed record PersonCategoriesInput(IReadOnlyList<PersonCategoryInput> Categories)
{
    /// <summary>Creates the editor input from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static PersonCategoriesInput From(ScrapeConfigurationEntity entity) =>
        new([.. entity.SearchConfiguration.PersonCategories.Select(category => new PersonCategoryInput(Option.Some(category.Id), category.Name))]);

    /// <summary>Validates the input: names must be non-blank and unique ignoring case, because names are matched case-insensitively. Names are trimmed.</summary>
    /// <returns>The validated <see cref="PersonCategoriesSettings"/>, or every validation error found.</returns>
    public Validation<PersonCategoriesSettings> Validate()
    {
        List<ValidationError> errors = [];
        HashSet<string> seenNames = new(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Categories.Count; i++)
        {
            var name = Categories[i].Name.Trim();
            if (name.Length == 0) errors.Add(ValidationErrorFactory.Create($"PersonCategories[{i}].Name", "Must not be blank."));
            else if (!seenNames.Add(name)) errors.Add(ValidationErrorFactory.Create($"PersonCategories[{i}].Name", $"Duplicate name '{name}'."));
        }

        return errors.Count > 0
            ? Validation.Invalid<PersonCategoriesSettings>(errors)
            : Validation.Valid(new PersonCategoriesSettings([.. Categories.Select(category => new PersonCategorySettings(category.Id, category.Name.Trim()))]));
    }
}
