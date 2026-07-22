using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Ensures every tagged category observed while scraping exists in the file classification taxonomy.
/// </summary>
public sealed class WallpaperCategoryRegistrar(IDbContextFactory<AppDbContext> dbContextFactory) : IWallpaperCategoryRegistrar
{
    /// <inheritdoc />
    public async Task EnsureCategoriesExistAsync(IReadOnlyList<TagData> tags, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var registeredNames = await ReadRegisteredNamesAsync(context, cancellationToken);

        foreach (var tag in tags.Where(HasCategoryAndTag).Select(TrimTagName))
        {
            if (!registeredNames.Add(tag.Tag))
                continue;

            context.FileClassificationCategories.Add(CreateCategory(tag));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool HasCategoryAndTag(TagData tag) => !string.IsNullOrWhiteSpace(tag.Category) && !string.IsNullOrWhiteSpace(tag.Tag);

    private static TagData TrimTagName(TagData tag) => tag with { Tag = tag.Tag.Trim() };

    private static async Task<HashSet<string>> ReadRegisteredNamesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var names = await context.FileClassificationCategories.Select(category => category.Name).ToListAsync(cancellationToken);

        return new HashSet<string>(names.Select(name => name.Trim()), StringComparer.OrdinalIgnoreCase);
    }

    private static FileClassificationCategoryEntity CreateCategory(TagData tag) => new()
    {
        Name = tag.Tag,
        IsFamous = tag.IsFamous,
        IsInternet = tag.IsInternet,
        IncludeInSearch = true,
    };
}
