using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Domain;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <inheritdoc />
public sealed class PersonCategoryCleanupService(IDbContextFactory<AppDbContext> dbContextFactory, IFileAutoCategorisor fileAutoCategorisor) : IPersonCategoryCleanupService
{
    private const string PersonCategoryName = "Person";

    /// <inheritdoc />
    public Task<Result<int, string>> RemoveInvalidPersonCategoriesAsync(CancellationToken cancellationToken = default)
        => Try.RunAsync(() => RemoveAsync(cancellationToken), cancellationToken).ToResultAsync(ex => ex.GetBaseException().Message);

    private async Task<int> RemoveAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        int personId = await db.FileClassificationCategories
            .Where(category => category.Level == 1 && category.ParentId == null && category.Name == PersonCategoryName)
            .Select(category => category.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (personId == 0)
            return 0;

        var children = await db.FileClassificationCategories
            .Where(category => category.Level == 2 && category.ParentId == personId)
            .Select(category => new { category.Id, category.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var childIds = children.Select(child => child.Id).ToList();

        var linkedPaths = await (from link in db.FileClassifications
                                 join item in db.SyncedItems on link.FileDetailId equals item.FileDetailId
                                 where childIds.Contains(link.CategoryId)
                                 select new { link.CategoryId, item.RemotePath })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var pathsByCategory = linkedPaths.ToLookup(linked => linked.CategoryId, linked => linked.RemotePath);

        var invalidIds = children
            .Where(child => pathsByCategory[child.Id].Any() && !pathsByCategory[child.Id].Any(path => YieldsPersonName(path, child.Name)))
            .Select(child => child.Id)
            .ToList();

        if (invalidIds.Count == 0)
            return 0;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        _ = await db.FileClassifications.Where(link => invalidIds.Contains(link.CategoryId)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        int removed = await db.FileClassificationCategories.Where(category => invalidIds.Contains(category.Id)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return removed;
    }

    private bool YieldsPersonName(string remotePath, string categoryName)
        => fileAutoCategorisor.Categorise(remotePath).Match(
            classification => classification.Level1 == PersonCategoryName && classification.Level2.MapOrDefault(level2 => string.Equals(level2, categoryName, StringComparison.OrdinalIgnoreCase), false),
            () => false);
}
