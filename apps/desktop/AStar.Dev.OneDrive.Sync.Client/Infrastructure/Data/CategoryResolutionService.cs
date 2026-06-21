using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;

/// <summary>Resolves file classification text levels to leaf category IDs, inserting missing nodes on demand.</summary>
public sealed class CategoryResolutionService(IDbContextFactory<AppDbContext> dbContextFactory) : ICategoryResolutionService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> ResolveManyAsync(IReadOnlyList<FileClassification> classifications, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entityLookup = await BuildEntityLookupAsync(context, cancellationToken).ConfigureAwait(false);

        List<FileClassificationCategoryEntity> leafEntities = [];
        foreach (var classification in classifications)
        {
            var leaf = ResolveClassification(context, entityLookup, classification);
            leafEntities.Add(leaf);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return [.. leafEntities.Select(e => e.Id).Distinct()];
    }

    private static async Task<Dictionary<(string Name, int Level, FileClassificationCategoryEntity? Parent), FileClassificationCategoryEntity>> BuildEntityLookupAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var categories = await context.FileClassificationCategories
            .Include(c => c.Parent)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return categories.ToDictionary(c => (c.Name, c.Level, c.Parent));
    }

    private static FileClassificationCategoryEntity ResolveClassification(AppDbContext context, Dictionary<(string Name, int Level, FileClassificationCategoryEntity? Parent), FileClassificationCategoryEntity> entityLookup, FileClassification classification)
    {
        var level1 = ResolveNode(context, entityLookup, classification.Level1, 1, null);

        if (classification.Level2 is not Option<string>.Some { Value: var level2Name })
            return level1;

        var level2 = ResolveNode(context, entityLookup, level2Name, 2, level1);

        if (classification.Level3 is not Option<string>.Some { Value: var level3Name })
            return level2;

        return ResolveNode(context, entityLookup, level3Name, 3, level2);
    }

    private static FileClassificationCategoryEntity ResolveNode(AppDbContext context, Dictionary<(string Name, int Level, FileClassificationCategoryEntity? Parent), FileClassificationCategoryEntity> entityLookup, string name, int level, FileClassificationCategoryEntity? parent)
    {
        var key = (name, level, parent);
        if (entityLookup.TryGetValue(key, out var existing))
            return existing;

        var entity = new FileClassificationCategoryEntity { Name = name, Level = level, Parent = parent };
        context.FileClassificationCategories.Add(entity);
        entityLookup[key] = entity;

        return entity;
    }
}
