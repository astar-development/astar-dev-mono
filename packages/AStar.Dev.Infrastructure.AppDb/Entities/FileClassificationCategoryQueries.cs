using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>Query and persistence helpers for <see cref="FileClassificationCategoryEntity"/> records.</summary>
public static class FileClassificationCategoryQueries
{
    private const string UnclassifiedCategoryName = "Unclassified";

    /// <summary>
    ///     Ensures each of the given category names exists as a Level 2 child of the "Unclassified" root
    ///     category, creating the root and any missing categories. Categories already present are left
    ///     untouched.
    /// </summary>
    /// <param name="context">The database context to persist through.</param>
    /// <param name="categoryNames">The category names observed during the scrape.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the operation.</param>
    public static async Task EnsureCategoriesExistAsync(this AppDbContext context, IReadOnlyList<string> categoryNames, CancellationToken cancellationToken = default)
    {
        var unclassified = await context.Set<FileClassificationCategoryEntity>()
            .SingleOrDefaultAsync(category => category.Level == 1 && category.ParentId == null && category.Name == UnclassifiedCategoryName, cancellationToken);

        if (unclassified is null)
        {
            unclassified = new FileClassificationCategoryEntity { Name = UnclassifiedCategoryName, Level = 1 };
            context.Set<FileClassificationCategoryEntity>().Add(unclassified);
            await context.SaveChangesAsync(cancellationToken);
        }

        var existingNames = await context.Set<FileClassificationCategoryEntity>()
            .Where(category => category.ParentId == unclassified.Id)
            .Select(category => category.Name)
            .ToListAsync(cancellationToken);

        var existingNameSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

        foreach (string? name in categoryNames.Where(name => !existingNameSet.Contains(name)))
        {
            context.Set<FileClassificationCategoryEntity>().Add(new FileClassificationCategoryEntity
            {
                Name = name,
                Level = 2,
                ParentId = unclassified.Id
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
