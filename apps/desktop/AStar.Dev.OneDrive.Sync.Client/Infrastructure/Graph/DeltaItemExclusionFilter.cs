using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

/// <summary>
/// Filters delta items that fall under explicitly-excluded folders so the sync engine
/// never downloads content from folders the user has opted out of.
/// </summary>
internal static class DeltaItemExclusionFilter
{
    /// <summary>
    /// Removes all items whose relative path falls under a folder whose ID is in
    /// <paramref name="excludedFolderIds"/>. Uses a two-pass approach: first resolve the
    /// relative paths of the excluded folders from the delta items themselves, then filter
    /// any item whose relative path is rooted at one of those paths.
    /// </summary>
    internal static List<DeltaItem> Filter(List<DeltaItem> items, IReadOnlySet<string> excludedFolderIds)
    {
        if(excludedFolderIds.Count == 0)
            return items;

        var excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(var item in items.Where(i => i.IsFolder && i.RelativePath is not null && excludedFolderIds.Contains(i.Id)))
            excludedPaths.Add(item.RelativePath!);

        if(excludedPaths.Count == 0)
            return items;

        return [.. items.Where(i => !IsUnderExcludedPath(i.RelativePath, excludedPaths))];
    }

    private static bool IsUnderExcludedPath(string? relativePath, HashSet<string> excludedPaths)
    {
        if(relativePath is null)
            return false;

        foreach(var excluded in excludedPaths)
        {
            if(relativePath.Equals(excluded, StringComparison.OrdinalIgnoreCase))
                return true;

            if(relativePath.StartsWith(excluded + "/", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
