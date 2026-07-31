using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

public sealed class SearchWorkflow(SearchResultsPage searchResultsPage, ScrapeConfiguration injectedScrapeConfiguration, ConfigurationSaver configurationSaver, ImagePageService imagePageService, IDirectoryHelper directoryHelper, ILogger logger, IDelayStrategy delayStrategy, TimeProvider timeProvider, PagedScrapeRunner pagedScrapeRunner)
{
    private SearchProgress progress = null!;

    public Task<Result<Unit, ScrapeError>> RunAsync(CancellationToken ct = default)
    {
        progress = SearchProgressFactory.Create(injectedScrapeConfiguration.SearchConfiguration, injectedScrapeConfiguration.ScrapeDirectories);
        var searchCategories = SearchProgressFunctions.FilterSearchCategories(progress.SearchConfiguration, progress.SearchConfiguration.SearchCategories);

        return ProcessSearchCategoriesAsync(searchCategories, ct).LogFailure(logger);
    }

    private async Task<Result<Unit, ScrapeError>> ProcessSearchCategoriesAsync(IReadOnlyList<Category> searchCategories, CancellationToken ct)
    {
        foreach (var searchCategory in searchCategories)
        {
            ct.ThrowIfCancellationRequested();

            var categoryResult = await ProcessSearchCategoryAsync(searchCategory, ct).ConfigureAwait(false);
            var categoryFailed = categoryResult.Match(_ => false, _ => true);

            if (categoryFailed) return categoryResult;
        }

        return Unit.Value;
    }

    private Task<Result<Unit, ScrapeError>> ProcessSearchCategoryAsync(Category searchCategory, CancellationToken ct)
    {
        string combinedSearchString = $"{progress.SearchConfiguration.SearchStringPrefix}{searchCategory.Id}{progress.SearchConfiguration.SearchStringSuffix}";
        progress = SearchProgressFunctions.UpdateSearchDetails(progress, combinedSearchString);

        return searchResultsPage.LoadSearchPageAsync(combinedSearchString, progress.SearchConfiguration.StartingPageNumber)
            .BindAsync(_ => searchResultsPage.PageInfoAsync())
            .BindAsync(pageInfo => ProcessCategoryPageInfoAsync(searchCategory, combinedSearchString, pageInfo, ct));
    }

    private async Task<Result<Unit, ScrapeError>> ProcessCategoryPageInfoAsync(Category searchCategory, string combinedSearchString, PageInfo pageInfo, CancellationToken ct)
    {
        progress = SearchProgressFunctions.UpdateTotalPages(progress, pageInfo.PageCount);

        if (searchCategory.IsUpToDate(pageInfo.ImageCount, pageInfo.PageCount))
        {
            logger.Information("{Category} is up to date (same image/page count), skipping...", searchCategory.Name);
            await delayStrategy.DelayAsync(DelayKind.CategoryUpToDate, ct).ConfigureAwait(false);

            return Unit.Value;
        }

        int startingPage = searchCategory.LastPageVisited > 0 ? searchCategory.LastPageVisited : 1;
        progress = progress with { SearchConfiguration = progress.SearchConfiguration with { StartingPageNumber = startingPage, }, };

        logger.Debug("Visiting {Category} from page {StartingPage} now...", searchCategory.Name, startingPage);
        progress = SearchProgressFunctions.UpdateSubDirectory(progress, pageInfo.SubDirectoryName);

        _ = directoryHelper.CreateDirectoryIfRequired([progress.ScrapeDirectories.RootDirectory.CombinePath(progress.ScrapeDirectories.BaseDirectory, pageInfo.SubDirectoryName),]);

        return await ProcessAllCategoryPagesAsync(searchCategory, combinedSearchString, ct)
            .BindAsync(_ => SaveCategoryProgressAsync(searchCategory, pageInfo))
            .ConfigureAwait(false);
    }

    private Task<Result<Unit, ScrapeError>> SaveCategoryProgressAsync(Category searchCategory, PageInfo pageInfo)
    {
        searchCategory.LastKnownImageCount = pageInfo.ImageCount;
        searchCategory.TotalPages = pageInfo.PageCount;
        searchCategory.LastPageVisited = 0;

        return configurationSaver.SaveUpdatedConfigurationAsync();
    }

    private async Task<Result<Unit, ScrapeError>> ProcessAllCategoryPagesAsync(Category searchCategory, string combinedSearchString, CancellationToken ct)
    {
        long startTimestamp = timeProvider.GetTimestamp();
        logger.Debug("About to visit the specific {Category} pages now...", searchCategory.Name);

        var plan = PagedScrapePlanFactory.Create(
            progress.SearchConfiguration.StartingPageNumber,
            progress.SearchConfiguration.TotalPages,
            pageNumber => RecordCategoryPageProgress(searchCategory, pageNumber),
            pageNumber => searchResultsPage.LoadSearchPageAsync(combinedSearchString, pageNumber),
            () => searchResultsPage.ImagePageLinksAsync(),
            (links, innerCt) => imagePageService.GetTheImagePagesAsync(links, searchCategory.Id, searchCategory.Name, innerCt));

        return (await pagedScrapeRunner.RunAsync(plan, ct).ConfigureAwait(false))
            .Tap(_ => logger.Information("Completed visiting the {Category}. Total time: {CategoryVisitDuration}", searchCategory.Name, timeProvider.GetElapsedTime(startTimestamp)));
    }

    private void RecordCategoryPageProgress(Category searchCategory, int pageNumber)
    {
        logger.Debug("About to visit page {page} (of {totalPages}) for {Category} now...", pageNumber, progress.SearchConfiguration.TotalPages, searchCategory.Name);
        progress = progress with { SearchConfiguration = progress.SearchConfiguration with { StartingPageNumber = pageNumber, }, };
        searchCategory.LastPageVisited = pageNumber;
    }
}
