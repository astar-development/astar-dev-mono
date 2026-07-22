using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Represents the one-shot configuration snapshot a scrape run needs, read once from the database before
///     any Playwright navigation begins.
/// </summary>
/// <param name="Categories">The search categories to visit, in the order they should be visited.</param>
/// <param name="ModelsToIgnore">Model names whose wallpapers should not be saved.</param>
/// <param name="TagsToIgnore">Tag names that should be dropped from a wallpaper's tag list.</param>
/// <param name="Directories">The directory naming conventions to use when saving wallpapers.</param>
/// <param name="FileClassifications">The file classification categories to use when saving wallpapers.</param>
public sealed record ScrapeContext(IReadOnlyList<ScrapeCategory> Categories, IReadOnlyList<string> ModelsToIgnore, IReadOnlyList<string> TagsToIgnore, DirectoryLayout Directories, IReadOnlyList<FileClassificationCategoryEntity> FileClassifications, SearchConfigurationEntity SearchConfiguration);
