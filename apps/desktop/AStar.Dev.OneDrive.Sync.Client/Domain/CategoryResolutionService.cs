using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Resolves file classification text levels to leaf category IDs, inserting missing nodes on demand.</summary>
public sealed class CategoryResolutionService(IDbContextFactory<AppDbContext> dbContextFactory) : ICategoryResolutionService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> ResolveManyAsync(IReadOnlyList<FileClassification> classifications, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var lookup = await BuildLookupAsync(context, cancellationToken).ConfigureAwait(false);

        HashSet<int> leafIds = [];
        foreach (var classification in classifications)
        {
            int leafId = await ResolveClassificationAsync(context, lookup, classification, cancellationToken).ConfigureAwait(false);
            _ = leafIds.Add(leafId);
        }

        return [..leafIds];
    }

    private static async Task<Dictionary<(string Name, int Level, int? ParentId), int>> BuildLookupAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var categories = await context.FileClassificationCategories
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return categories.ToDictionary(c => (c.Name, c.Level, c.ParentId), c => c.Id);
    }

    private static async Task<int> ResolveClassificationAsync(AppDbContext context, Dictionary<(string Name, int Level, int? ParentId), int> lookup, FileClassification classification, CancellationToken cancellationToken)
    {
        int level1Id = await ResolveNodeAsync(context, lookup, classification.Level1, 1, null, cancellationToken).ConfigureAwait(false);

        if (classification.Level2 is not Option<string>.Some { Value: var level2Name })
            return level1Id;

        int level2Id = await ResolveNodeAsync(context, lookup, level2Name, 2, level1Id, cancellationToken).ConfigureAwait(false);

        if (classification.Level3 is not Option<string>.Some { Value: var level3Name })
            return level2Id;

        return await ResolveNodeAsync(context, lookup, level3Name, 3, level2Id, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> ResolveNodeAsync(AppDbContext context, Dictionary<(string Name, int Level, int? ParentId), int> lookup, string name, int level, int? parentId, CancellationToken cancellationToken)
    {
        var key = (name, level, parentId);
        if (lookup.TryGetValue(key, out int existingId))
            return existingId;

        var entity = new FileClassificationCategoryEntity { Name = name, Level = level, ParentId = parentId };
        context.FileClassificationCategories.Add(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        lookup[key] = entity.Id;

        return entity.Id;
    }
}
