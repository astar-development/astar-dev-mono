using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

/// <summary>Pure state transition functions for a <see cref="SearchProgress" />, frozen from the historical <c>SearchWorkflow</c> private methods.</summary>
public static class SearchProgressFunctions
{
    /// <summary>Skips categories preceding the one matching the current search string, frozen from the historical <c>FilterSearchCategories</c> behaviour.</summary>
    public static IReadOnlyList<Category> FilterSearchCategories(SearchConfiguration searchConfiguration, IReadOnlyList<Category> searchCategories)
    {
        for (int i = 0; i < searchCategories.Count; i++)
        {
            string combinedSearchString = $"{searchConfiguration.SearchStringPrefix}{searchCategories[i].Id}{searchConfiguration.SearchStringSuffix}";

            if (combinedSearchString != searchConfiguration.SearchString) continue;

            return [.. searchCategories.Skip(i),];
        }

        return searchCategories;
    }

    /// <summary>Updates the search string and resets the starting page number when the combined search string has changed.</summary>
    public static SearchProgress UpdateSearchDetails(SearchProgress progress, string combinedSearchString)
    {
        if (progress.SearchConfiguration.SearchString == combinedSearchString) return progress;

        return progress with { SearchConfiguration = progress.SearchConfiguration with { StartingPageNumber = 1, SearchString = combinedSearchString, }, };
    }

    /// <summary>Updates the total page count when it has changed.</summary>
    public static SearchProgress UpdateTotalPages(SearchProgress progress, int pageCount)
    {
        if (progress.SearchConfiguration.TotalPages == pageCount) return progress;

        return progress with { SearchConfiguration = progress.SearchConfiguration with { TotalPages = pageCount, }, };
    }

    /// <summary>Updates the sub-directory name when a non-empty name is supplied.</summary>
    public static SearchProgress UpdateSubDirectory(SearchProgress progress, string subDirectoryName)
    {
        if (subDirectoryName.Length == 0) return progress;

        return progress with { ScrapeDirectories = progress.ScrapeDirectories with { SubDirectoryName = subDirectoryName, }, };
    }
}
