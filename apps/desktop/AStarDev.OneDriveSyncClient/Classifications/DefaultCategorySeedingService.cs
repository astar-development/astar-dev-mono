using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <inheritdoc />
public sealed class DefaultCategorySeedingService(IDbContextFactory<AppDbContext> dbContextFactory) : IDefaultCategorySeedingService
{
    /// <inheritdoc />
    public Task<Result<int, string>> SeedDefaultCategoriesAsync(CancellationToken cancellationToken = default)
        => Try.RunAsync(() => SeedAsync(cancellationToken), cancellationToken).ToResultAsync(ex => ex.GetBaseException().Message);

    private async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await db.FileClassificationCategories.Where(category => category.Level <= 2).ToListAsync(cancellationToken).ConfigureAwait(false);
        int added = 0;

        foreach ((string rootName, bool includeInSearch, var childNames) in DefaultClassificationTaxonomy.Roots)
        {
            var root = existing.FirstOrDefault(category => category.Level == 1 && category.ParentId is null && IsSameName(category.Name, rootName));

            if (root is null)
            {
                root = new FileClassificationCategoryEntity { Name = rootName, Level = 1, IncludeInSearch = includeInSearch };
                db.FileClassificationCategories.Add(root);
                existing.Add(root);
                added++;
            }

            foreach (string childName in childNames)
            {
                if (existing.Any(category => category.Level == 2 && IsSameName(category.Name, childName) && IsChildOf(category, root)))
                    continue;

                var child = new FileClassificationCategoryEntity { Name = childName, Level = 2, Parent = root, IncludeInSearch = root.IncludeInSearch };
                db.FileClassificationCategories.Add(child);
                existing.Add(child);
                added++;
            }
        }

        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return added;
    }

    private static bool IsSameName(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static bool IsChildOf(FileClassificationCategoryEntity category, FileClassificationCategoryEntity parent) => ReferenceEquals(category.Parent, parent) || (parent.Id != 0 && category.ParentId == parent.Id);
}
