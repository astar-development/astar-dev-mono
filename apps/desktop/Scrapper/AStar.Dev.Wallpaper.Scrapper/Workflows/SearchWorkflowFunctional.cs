using System.Diagnostics;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Playwright;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

public sealed class SearchWorkflowFunctional(SearchResultsPageFunctional searchResultsPageFunctional, ScrapeConfiguration injectedScrapeConfiguration, ConfigurationSaver configurationSaver, ImagePageService imagePageService, ILogger logger)
{
    private ScrapeConfiguration scrapeConfiguration = null!;
    private SearchConfiguration searchConfiguration = null!;
    private ScrapeDirectories   scrapeDirectories = null!;

    public async Task<Result<Unit, string>> RunAsync(ILogger scrapeLogger, CancellationToken ct = default)
    {
        try
        {
            scrapeConfiguration = injectedScrapeConfiguration;
            searchConfiguration = scrapeConfiguration.SearchConfiguration;
            scrapeDirectories = scrapeConfiguration.ScrapeDirectories;
            List<Category> searchCategories = FilterSearchCategories([.. searchConfiguration.SearchCategories]);
            await ProcessSearchCategories([.. searchConfiguration.SearchCategories], scrapeLogger, ct);

            return Unit.Value;
        }
        catch(Exception exception) when (exception is not OperationCanceledException)
        {
            scrapeLogger.Error(exception.GetBaseException().Message);
            throw;
        }
    }

    private async Task ProcessSearchCategories(List<Category> searchCategories, ILogger scrapeLogger, CancellationToken ct)
    {
        foreach(Category searchCategory in searchCategories)
        {
            ct.ThrowIfCancellationRequested();
            var combinedSearchString = $"{searchConfiguration.SearchStringPrefix}{searchCategory.Id}{searchConfiguration.SearchStringSuffix}";

            searchConfiguration = UpdateSearchDetailsIfRequired(combinedSearchString);

            IResponse? pageDetails = await searchResultsPageFunctional.LoadSearchPageAsync(combinedSearchString, searchConfiguration.StartingPageNumber);

            if(pageDetails is { Ok: false, }) throw new InvalidOperationException("Could not get the image page after retry...");

            var (pageCount, imageCount, subDirectoryName) = await searchResultsPageFunctional.PageInfoAsync();
            UpdateSearchTotalPagesIfRequired(pageCount);

            if(searchCategory.IsUpToDate(imageCount, pageCount))
            {
                logger.Information("{Category} is up to date (same image/page count), skipping...", searchCategory.Name);
                await Task.Delay(TimeSpan.FromSeconds(RandomDelay()), ct);
                continue;
            }

            var startingPage = searchCategory.LastPageVisited > 0 ? searchCategory.LastPageVisited : 1;
            searchConfiguration = searchConfiguration with { StartingPageNumber = startingPage };

            logger.Debug("Visiting {Category} from page {StartingPage} now...", searchCategory.Name, startingPage);
            scrapeDirectories = UpdateSubDirectoryIfRequired(subDirectoryName);

            _ = DirectoryHelper.CreateDirectoryIfRequired([Path.Combine(scrapeDirectories.RootDirectory, scrapeDirectories.BaseDirectory, subDirectoryName)]);

            await ProcessAllCategoryPages(searchCategory, combinedSearchString, scrapeLogger, ct);

            searchCategory.LastKnownImageCount = imageCount;
            searchCategory.TotalPages          = pageCount;
            searchCategory.LastPageVisited     = 0;
            await configurationSaver.SaveUpdatedConfigurationAsync();
        }
    }

    private static int RandomDelay() => new Random().Next(1, 5);

    private async Task ProcessAllCategoryPages(Category searchCategory, string combinedSearchString, ILogger scrapeLogger, CancellationToken ct)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        scrapeLogger.Debug("About to visit the specific {Category} pages now...", searchCategory.Name);

        for(var currentPageNumber = searchConfiguration.StartingPageNumber; currentPageNumber <= searchConfiguration.TotalPages; currentPageNumber++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            scrapeLogger.Debug("About to visit page {page} (of {totalPages}) for {Category} now...", currentPageNumber, searchConfiguration.TotalPages, searchCategory.Name);
            searchConfiguration = searchConfiguration with { StartingPageNumber = currentPageNumber };
            searchCategory.LastPageVisited          = currentPageNumber;
            await configurationSaver.SaveUpdatedConfigurationAsync();
            _ = await searchResultsPageFunctional.LoadSearchPageAsync(combinedSearchString, currentPageNumber);

            IReadOnlyCollection<string> imagePageLinks = await searchResultsPageFunctional.ImagePageLinksAsync();
            await imagePageService.GetTheImagePagesAsync(imagePageLinks, searchCategory.Id, searchCategory.Name, ct);
        }

        stopwatch.Stop();
        scrapeLogger.Information("Completed visiting the {Category}. Total time: {CategoryVisitDuration}", searchCategory.Name, stopwatch.Elapsed);
    }

    private ScrapeDirectories UpdateSubDirectoryIfRequired(string subDirectoryName)
    {
        if(scrapeDirectories is null) scrapeDirectories = new ScrapeDirectories(scrapeConfiguration.ScrapeDirectories.RootDirectory, scrapeConfiguration.ScrapeDirectories.BaseSaveDirectory, scrapeConfiguration.ScrapeDirectories.BaseDirectory, scrapeConfiguration.ScrapeDirectories.BaseDirectoryFamous, subDirectoryName);
        else if(subDirectoryName.Length > 0) scrapeDirectories = scrapeDirectories with { SubDirectoryName = subDirectoryName };

        return scrapeDirectories;
    }

    private SearchConfiguration UpdateSearchDetailsIfRequired(string combinedSearchString)
    {
        if(searchConfiguration.SearchString == combinedSearchString) return searchConfiguration;
        searchConfiguration = searchConfiguration with { StartingPageNumber = 1, SearchString = combinedSearchString };
        return searchConfiguration;
    }

    private List<Category> FilterSearchCategories(List<Category> searchCategories)
    {
        for(var i = 0; i < searchCategories.Count; i++)
        {
            var combinedSearchString = $"{searchConfiguration.SearchStringPrefix}{searchCategories[i].Id}{searchConfiguration.SearchStringSuffix}";

            if(combinedSearchString != searchConfiguration.SearchString) continue;

            searchCategories = [.. searchCategories.Skip(i)];
            break;
        }

        return searchCategories;
    }

    private void UpdateSearchTotalPagesIfRequired(int pageCount)
    {
        if(searchConfiguration.TotalPages != pageCount) searchConfiguration = searchConfiguration with { TotalPages = pageCount };
    }
}
