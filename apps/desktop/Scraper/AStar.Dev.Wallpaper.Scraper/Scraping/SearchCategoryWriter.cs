using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <inheritdoc cref="ISearchCategoryWriter" />
public sealed class SearchCategoryWriter(IDbContextFactory<AppDbContext> dbContextFactory) : ISearchCategoryWriter
{
    /// <inheritdoc cref="ISearchCategoryWriter.WriteAsync" />
    public async Task<Result<Unit, string>> WriteAsync(SearchCategoryDto searchCategory, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existingCategory = await context.SearchCategories.FirstOrDefaultAsync(category => category.Name == searchCategory.Name, cancellationToken);

        if (existingCategory is null)
            return Result.Failure<Unit, string>($"No search category named '{searchCategory.Name}' exists to update.");

        existingCategory.IsFamous = searchCategory.IsFamous;
        existingCategory.IsInternet = searchCategory.IsInternet;
        existingCategory.TotalPages = searchCategory.TotalPages;
        existingCategory.LastKnownImageCount = searchCategory.LastKnownImageCount;
        existingCategory.LastPageVisited = searchCategory.LastPageVisited;
        existingCategory.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success<Unit, string>(Unit.Instance);
    }
}
