using AStar.Dev.Guard.Clauses;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>Pure category-merge logic, frozen from the historical <c>ConfigurationSaver.UpdateAndSaveTheConfigurationAsync</c> upsert loop.</summary>
public static class CategoryMerger
{
    /// <summary>
    ///     Partitions <paramref name="updated" /> (de-duplicated by <see cref="Category.Id" />, keeping the first
    ///     occurrence) into categories that match an id already in <paramref name="existing" /> and categories that
    ///     do not.
    /// </summary>
    public static (IReadOnlyList<Category> ToUpdate, IReadOnlyList<Category> ToAdd) MergeCategories(
        IEnumerable<(string Id, string Name, int LastKnownImageCount, int LastPageVisited, int TotalPages)> existing,
        IReadOnlyList<Category> updated)
    {
        GuardAgainst.Null(existing);
        GuardAgainst.Null(updated);

        var existingIds = existing.Select(category => category.Id).ToHashSet();
        var dedupedUpdated = updated.DistinctBy(category => category.Id).ToList();

        IReadOnlyList<Category> toUpdate = [.. dedupedUpdated.Where(category => existingIds.Contains(category.Id)),];
        IReadOnlyList<Category> toAdd = [.. dedupedUpdated.Where(category => !existingIds.Contains(category.Id)),];

        return (toUpdate, toAdd);
    }
}
