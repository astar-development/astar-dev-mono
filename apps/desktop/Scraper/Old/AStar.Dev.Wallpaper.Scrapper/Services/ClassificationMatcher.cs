using AStar.Dev.Guard.Clauses;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Support;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

/// <summary>
///     Pure classification matching, frozen from the historical <c>FileClassificationService.CollectFileNameMatches</c>
///     and category-classification steps. Tag matches are excluded because resolving them requires a database lookup.
/// </summary>
public static class ClassificationMatcher
{
    /// <summary>Matches <paramref name="pageData" />'s searchable classifications and category classification against <paramref name="fileDetail" />'s name.</summary>
    public static IReadOnlyList<FileClassificationCategoryEntity> Match(PageClassificationData pageData, FileDetailEntity fileDetail)
    {
        GuardAgainst.Null(pageData);
        GuardAgainst.Null(fileDetail);

        var fileNameMatches = pageData.SearchableClassifications
            .Where(entry => entry.Keywords.Any(keyword => fileDetail.FullNameWithPath().Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .Select(entry => entry.Category);

        return pageData.CategoryClassification is null
            ? [.. fileNameMatches,]
            : [.. fileNameMatches, pageData.CategoryClassification,];
    }
}
