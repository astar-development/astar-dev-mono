using System.Diagnostics;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Playwright;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

public sealed class SearchWorkflow(
    SearchResultsPage searchResultsPage,
    ImagePageService imagePageService,
    SearchConfiguration searchConfiguration,
    ScrapeDirectories scrapeDirectories,
    ConfigurationSaver configurationSaver,
    Logger logger)
{
    private ScrapeDirectories _scrapeDirectories = scrapeDirectories ?? throw new ArgumentNullException(nameof(scrapeDirectories));
    private SearchConfiguration _searchConfiguration = searchConfiguration ?? throw new ArgumentNullException(nameof(searchConfiguration));

    public async Task RunAsync(CancellationToken ct = default)
    {
        try
        {
            var searchCategories = FilterSearchCategories([.. _searchConfiguration.SearchCategories]);
            await ProcessSearchCategories([.. _searchConfiguration.SearchCategories], ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(exception.GetBaseException().Message);
            throw;
        }
    }

    private async Task ProcessSearchCategories(List<Category> searchCategories, CancellationToken ct)
    {
        foreach (var searchCategory in searchCategories)
        {
            ct.ThrowIfCancellationRequested();
            string combinedSearchString = $"{_searchConfiguration.SearchStringPrefix}{searchCategory.Id}{_searchConfiguration.SearchStringSuffix}";

            _searchConfiguration = UpdateSearchDetailsIfRequired(combinedSearchString);

            var pageDetails = await searchResultsPage.LoadSearchPageAsync(combinedSearchString, _searchConfiguration.StartingPageNumber);

            if (pageDetails is { Ok: false, }) throw new InvalidOperationException("Could not get the image page after retry...");

            var (pageCount, imageCount, subDirectoryName) = await searchResultsPage.PageInfoAsync();
            UpdateSearchTotalPagesIfRequired(pageCount);

            if (SearchCategoryHasBeenFullyVisited(combinedSearchString, searchCategory, imageCount))
            {
                logger.Debug("{Category} category has been fully visited...", searchCategory.Name);
                continue;
            }

            int startingPage = searchCategory.LastPageVisited > 0 ? searchCategory.LastPageVisited : 1;
            _searchConfiguration = _searchConfiguration with { StartingPageNumber = startingPage };

            logger.Debug("Visiting {Category} from page {StartingPage} now...", searchCategory.Name, startingPage);
            _scrapeDirectories = UpdateSubDirectoryIfRequired(subDirectoryName);

            await ProcessAllCategoryPages(searchCategory, combinedSearchString, ct);

            searchCategory.LastKnownImageCount = imageCount;
            searchCategory.LastPageVisited = 0;
            await configurationSaver.SaveUpdatedConfigurationAsync();
        }
    }

    private async Task ProcessAllCategoryPages(Category searchCategory, string combinedSearchString, CancellationToken ct)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        logger.Debug("About to visit the specific {Category} pages now...", searchCategory.Name);

        for (int currentPageNumber = _searchConfiguration.StartingPageNumber; currentPageNumber <= _searchConfiguration.TotalPages; currentPageNumber++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            logger.Debug("About to visit page {page} (of {totalPages}) for {Category} now...", currentPageNumber, _searchConfiguration.TotalPages, searchCategory.Name);
            _searchConfiguration = _searchConfiguration with { StartingPageNumber = currentPageNumber };
            searchCategory.LastPageVisited = currentPageNumber;
            await configurationSaver.SaveUpdatedConfigurationAsync();
            _ = await searchResultsPage.LoadSearchPageAsync(combinedSearchString, currentPageNumber);

            var imagePageLinks = await searchResultsPage.ImagePageLinksAsync();
            await imagePageService.GetTheImagePagesAsync(imagePageLinks, searchCategory.Id, searchCategory.Name, ct);
        }

        stopwatch.Stop();
        logger.Information("Completed visiting the {Category}. Total time: {CategoryVisitDuration}", searchCategory.Name, stopwatch.Elapsed);
    }

    private ScrapeDirectories UpdateSubDirectoryIfRequired(string subDirectoryName)
    {
        if (subDirectoryName.Length > 0) _scrapeDirectories = _scrapeDirectories with { SubDirectoryName = subDirectoryName };
        return _scrapeDirectories;
    }

    private SearchConfiguration UpdateSearchDetailsIfRequired(string combinedSearchString)
    {
        if (_searchConfiguration.SearchString == combinedSearchString) return _searchConfiguration;
        _searchConfiguration = _searchConfiguration with { StartingPageNumber = 1, SearchString = combinedSearchString };
        return _searchConfiguration;
    }

    private bool SearchCategoryHasBeenFullyVisited(string combinedSearchString, Category searchCategory, int imageCount)
        => _searchConfiguration.SearchString == combinedSearchString && searchCategory.LastKnownImageCount == imageCount;

    private List<Category> FilterSearchCategories(List<Category> searchCategories)
    {
        for (int i = 0; i < searchCategories.Count; i++)
        {
            string combinedSearchString = $"{_searchConfiguration.SearchStringPrefix}{searchCategories[i].Id}{_searchConfiguration.SearchStringSuffix}";

            if (combinedSearchString != _searchConfiguration.SearchString) continue;

            searchCategories = [.. searchCategories.Skip(i)];
            break;
        }

        return searchCategories;
    }

    private void UpdateSearchTotalPagesIfRequired(int pageCount)
    {
        if (_searchConfiguration.TotalPages != pageCount) _searchConfiguration = _searchConfiguration with { TotalPages = pageCount };
    }
}
