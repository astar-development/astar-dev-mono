using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>The unvalidated contents of every section of the configuration editor.</summary>
/// <param name="Root">The root-level settings.</param>
/// <param name="User">The user settings.</param>
/// <param name="Directories">The scrape directories.</param>
/// <param name="Search">The search settings.</param>
/// <param name="SearchCategories">The search categories.</param>
/// <param name="PersonCategories">The person categories.</param>
public sealed record ConfigurationEditInputs(
    RootSettingsInput Root,
    UserSettingsInput User,
    DirectorySettingsInput Directories,
    SearchSettingsInput Search,
    SearchCategoriesInput SearchCategories,
    PersonCategoriesInput PersonCategories)
{
    /// <summary>Reads every section of <paramref name="entity"/> into editor inputs.</summary>
    /// <param name="entity">The scrape configuration to edit.</param>
    public static ConfigurationEditInputs From(ScrapeConfigurationEntity entity) => new(
        RootSettingsInput.From(entity),
        UserSettingsInput.From(entity),
        DirectorySettingsInput.From(entity),
        SearchSettingsInput.From(entity),
        SearchCategoriesInput.From(entity),
        PersonCategoriesInput.From(entity));
}
