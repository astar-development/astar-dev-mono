using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Reads the one-shot configuration snapshot a scrape run needs, before any Playwright navigation begins.
/// </summary>
public sealed class ScrapeContextReader(IDbContextFactory<AppDbContext> dbContextFactory) : IScrapeContextReader
{
    /// <inheritdoc />
    public async Task<ScrapeContext> ReadAsync(CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var searchConfiguration = await ReadSearchConfigurationAsync(context, cancellationToken);
        var categories = await ReadCategoriesAsync(context, searchConfiguration, cancellationToken);
        var modelsToIgnore = await ReadModelsToIgnoreAsync(context, cancellationToken);
        var tagsToIgnore = await ReadTagsToIgnoreAsync(context, cancellationToken);
        var directoryLayout = await ReadDirectoryLayoutAsync(context, cancellationToken);
        var fileClassifications = (IReadOnlyList<FileClassificationCategoryEntity>)await context.FileClassificationCategories.ToListAsync(cancellationToken);

        return new ScrapeContext(categories, modelsToIgnore, tagsToIgnore, directoryLayout, fileClassifications, searchConfiguration);
    }

    private static Task<SearchConfigurationEntity> ReadSearchConfigurationAsync(AppDbContext context, CancellationToken cancellationToken) =>
        context.SearchConfigurations.OrderByDescending(configuration => configuration.UpdatedAt).FirstAsync(cancellationToken);

    private static async Task<IReadOnlyList<ScrapeCategory>> ReadCategoriesAsync(AppDbContext context, SearchConfigurationEntity searchConfiguration, CancellationToken cancellationToken)
    {
        var categories = await context.SearchCategories.Where(category => category.IncludeInSearch).OrderByDescending(category => category.IsFamous).ThenByDescending(category => category.IsInternet).ThenBy(category => category.Name).ToListAsync(cancellationToken);

        return [.. categories.Select(category => new ScrapeCategory(category.Name, $"{searchConfiguration.SearchStringPrefix}{category.Id}{searchConfiguration.SearchStringSuffix}", category.IsFamous, category.IsInternet))];
    }

    private static async Task<IReadOnlyList<string>> ReadModelsToIgnoreAsync(AppDbContext context, CancellationToken cancellationToken) =>
        await context.ModelsToIgnore.Select(model => model.Value).ToListAsync(cancellationToken);

    private static async Task<IReadOnlyList<string>> ReadTagsToIgnoreAsync(AppDbContext context, CancellationToken cancellationToken) =>
        await context.TagsToIgnore.Select(tag => tag.Value).ToListAsync(cancellationToken);

    private static async Task<DirectoryLayout> ReadDirectoryLayoutAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var directories = await context.ScrapeDirectories.OrderByDescending(directory => directory.CreatedAt).FirstOrDefaultAsync(cancellationToken);

        return new DirectoryLayout(directories?.RootDirectory ?? string.Empty, directories?.BaseDirectory ?? string.Empty, directories?.BaseDirectoryFamous ?? string.Empty);
    }
}
