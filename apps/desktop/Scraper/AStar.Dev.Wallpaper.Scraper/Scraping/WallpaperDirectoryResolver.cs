using System.IO.Abstractions;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Resolves the directory a wallpaper should be saved under, from its curated tags.
/// </summary>
public static class WallpaperDirectoryResolver
{
    private const int MaxTagSegments = 12;
    private const int DrivePrefixLength = 3;

    /// <summary>
    ///     Builds the directory path for a wallpaper, nesting one path segment per categorised tag: famous and
    ///     internet-model tags first, then the remaining tags in alphabetical order. Tags without a category, or
    ///     matching the search category's name, contribute no path segment. At most twelve tag segments are used.
    ///     If a directory already exists on disk under the base category directory whose segments are all present
    ///     in the computed tag set (no unrelated segments), that directory's existing order is reused rather than
    ///     creating a differently ordered duplicate, preferring the deepest such directory. Segments no longer
    ///     present in the computed set are dropped, and newly eligible segments are appended to the end, ordered by
    ///     <see cref="FileClassificationCategoryEntity.Level" /> ascending then
    ///     <see cref="FileClassificationCategoryEntity.Priority" /> ascending.
    /// </summary>
    /// <param name="directoryLayout">The directory naming conventions to use.</param>
    /// <param name="tags">The wallpaper's curated tags.</param>
    /// <param name="category">The search category the wallpaper was found under.</param>
    /// <param name="fileClassifications">The known file classification categories used to order and name path segments.</param>
    /// <param name="fileSystem">The file system used to detect an existing directory to reuse.</param>
    public static string Resolve(DirectoryLayout directoryLayout, IReadOnlyList<TagData> tags, ScrapeCategory category, IReadOnlyList<FileClassificationCategoryEntity> fileClassifications, IFileSystem fileSystem)
    {
        string baseDirectoryWithCategory = SetBaseDirectory(directoryLayout, tags, category);
        var orderedFileClassifications = FilterTagsAndRetrieveFileClassifications(tags, category, fileClassifications);
        string path = BuildDirectoryPath(baseDirectoryWithCategory, orderedFileClassifications, fileSystem);
        int drivePrefixLength = Math.Min(DrivePrefixLength, path.Length);

        return path[..drivePrefixLength] + path[drivePrefixLength..].Replace(":", string.Empty).CleanPath();
    }

    private static string BuildDirectoryPath(string baseDirectoryWithCategory, IReadOnlyList<FileClassificationCategoryEntity> orderedFileClassifications, IFileSystem fileSystem)
    {
        var existingSegments = FindBestMatchingExistingSegments(baseDirectoryWithCategory, orderedFileClassifications, fileSystem);

        if (existingSegments is null)
            return orderedFileClassifications.Aggregate(baseDirectoryWithCategory, (directory, classification) => directory.CombinePath(classification.Name));

        var retainedNames = new HashSet<string>(existingSegments, StringComparer.OrdinalIgnoreCase);
        var newSegments = orderedFileClassifications
            .Where(classification => !retainedNames.Contains(classification.Name))
            .OrderBy(classification => classification.Level)
            .ThenBy(classification => classification.Priority);
        string reusedDirectory = existingSegments.Aggregate(baseDirectoryWithCategory, (directory, segment) => directory.CombinePath(segment));

        return newSegments.Aggregate(reusedDirectory, (directory, classification) => directory.CombinePath(classification.Name));
    }

    private static List<string>? FindBestMatchingExistingSegments(string baseDirectoryWithCategory, IReadOnlyList<FileClassificationCategoryEntity> orderedFileClassifications, IFileSystem fileSystem)
    {
        if (!fileSystem.Directory.Exists(baseDirectoryWithCategory))
            return null;

        var computedNames = new HashSet<string>(orderedFileClassifications.Select(classification => classification.Name), StringComparer.OrdinalIgnoreCase);
        List<string>? bestSegments = null;

        foreach (string? directory in fileSystem.Directory.GetDirectories(baseDirectoryWithCategory, "*", SearchOption.AllDirectories).OrderBy(directory => directory, StringComparer.Ordinal))
        {
            string[] segments = fileSystem.Path.GetRelativePath(baseDirectoryWithCategory, directory).Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

            if (segments.Any(segment => !computedNames.Contains(segment)))
                continue;

            if (bestSegments is null || segments.Length > bestSegments.Count)
                bestSegments = [.. segments];
        }

        return bestSegments;
    }

    private static IReadOnlyList<FileClassificationCategoryEntity> FilterTagsAndRetrieveFileClassifications(IReadOnlyList<TagData> tags, ScrapeCategory category, IReadOnlyList<FileClassificationCategoryEntity> fileClassifications)
    {
        var eligibleTags = tags.Where(tag => tag.Category is not null && !tag.Tag.Equals(category.Name, StringComparison.OrdinalIgnoreCase)).ToList();

        return [.. GetOrderedFileClassifications(fileClassifications, eligibleTags).Take(MaxTagSegments)];
    }

    private static string SetBaseDirectory(DirectoryLayout directoryLayout, IReadOnlyList<TagData> tags, ScrapeCategory category)
    {
        string baseDirectory = directoryLayout.RootDirectory + (tags.Any(tag => tag.IsFamous) ? directoryLayout.BaseDirectoryFamous : directoryLayout.BaseDirectory);

        return baseDirectory.CombinePath(category.Name[0].ToString()).CombinePath(category.Name);
    }

    private static IReadOnlyList<FileClassificationCategoryEntity> GetOrderedFileClassifications(IReadOnlyList<FileClassificationCategoryEntity> fileClassifications, List<TagData> eligibleTags)
        => [.. fileClassifications
                .Where(classification => classification.IncludeInSearch && eligibleTags.Any(tag => tag.Tag.CaseInsensitiveEquals(classification.Name)))
                .OrderByDescending(t => t.IsFamous)
                .ThenByDescending(t => t.IsInternet)
                .ThenBy(t => t.Level)];
}
