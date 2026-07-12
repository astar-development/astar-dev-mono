using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Pages;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Support;
using Serilog;

namespace AStar.Dev.Wallpaper.Scraper.Workflows;

public sealed class SearchWorkflow(SearchResultsPage searchResultsPage, ScrapeConfiguration injectedScrapeConfiguration, ConfigurationSaver configurationSaver, ImagePageService imagePageService, IDirectoryHelper directoryHelper, ILogger logger, IDelayStrategy delayStrategy, TimeProvider timeProvider, PagedScrapeRunner pagedScrapeRunner)
{
    public Task<Result<Unit, ScrapeError>> RunAsync(CancellationToken cancellationToken = default)
    {
        var progress = SearchProgressFactory.Create(injectedScrapeConfiguration.SearchConfiguration, injectedScrapeConfiguration.ScrapeDirectories);
        var searchCategories = SearchProgressFunctions.FilterSearchCategories(progress.SearchConfiguration, progress.SearchConfiguration.SearchCategories);

        return ProcessSearchCategoriesAsync(progress, searchCategories, cancellationToken).LogFailure(logger);
    }

    private async Task<Result<Unit, ScrapeError>> ProcessSearchCategoriesAsync(SearchProgress progress, IReadOnlyList<Category> searchCategories, CancellationToken cancellationToken)
    {
        Result<Unit, ScrapeError> outcome = Unit.Value;

        foreach (var searchCategory in searchCategories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            outcome = await outcome.BindAsync(_ => ProcessSearchCategoryAsync(progress, searchCategory, cancellationToken)).ConfigureAwait(false);
        }

        return outcome;
    }

    private Task<Result<Unit, ScrapeError>> ProcessSearchCategoryAsync(SearchProgress progress, Category searchCategory, CancellationToken cancellationToken)
    {
        string combinedSearchString = $"{progress.SearchConfiguration.SearchStringPrefix}{searchCategory.Id}{progress.SearchConfiguration.SearchStringSuffix}";
        var updatedProgress = SearchProgressFunctions.UpdateSearchDetails(progress, combinedSearchString);

        return searchResultsPage.LoadSearchPageAsync(combinedSearchString, updatedProgress.SearchConfiguration.StartingPageNumber)
            .BindAsync(_ => searchResultsPage.PageInfoAsync())
            .BindAsync(pageInfo => ProcessCategoryPageInfoAsync(updatedProgress, searchCategory, combinedSearchString, pageInfo, cancellationToken));
    }

    private async Task<Result<Unit, ScrapeError>> ProcessCategoryPageInfoAsync(SearchProgress progress, Category searchCategory, string combinedSearchString, PageInfo pageInfo, CancellationToken cancellationToken)
    {
        var updatedProgress = SearchProgressFunctions.UpdateTotalPages(progress, pageInfo.PageCount);

        if (searchCategory.IsUpToDate(pageInfo.ImageCount, pageInfo.PageCount))
        {
            logger.Information("{Category} is up to date (same image/page count), skipping...", searchCategory.Name);
            await delayStrategy.DelayAsync(DelayKind.CategoryUpToDate, cancellationToken).ConfigureAwait(false);

            return Unit.Value;
        }

        int startingPage = searchCategory.LastPageVisited > 0 ? searchCategory.LastPageVisited : 1;
        updatedProgress = updatedProgress with { SearchConfiguration = updatedProgress.SearchConfiguration with { StartingPageNumber = startingPage, }, };

        logger.Debug("Visiting {Category} from page {StartingPage} now...", searchCategory.Name, startingPage);
        updatedProgress = SearchProgressFunctions.UpdateSubDirectory(updatedProgress, pageInfo.SubDirectoryName);

        _ = directoryHelper.CreateDirectoryIfRequired([updatedProgress.ScrapeDirectories.RootDirectory.CombinePath(updatedProgress.ScrapeDirectories.BaseDirectory, pageInfo.SubDirectoryName),]);

        return await ProcessAllCategoryPagesAsync(updatedProgress, searchCategory, combinedSearchString, cancellationToken)
            .BindAsync(_ => SaveCategoryProgressAsync(searchCategory, pageInfo, cancellationToken))
            .ConfigureAwait(false);
    }

    private Task<Result<Unit, ScrapeError>> SaveCategoryProgressAsync(Category searchCategory, PageInfo pageInfo, CancellationToken cancellationToken)
    {
        searchCategory.LastKnownImageCount = pageInfo.ImageCount;
        searchCategory.TotalPages = pageInfo.PageCount;
        searchCategory.LastPageVisited = 0;

        return configurationSaver.SaveUpdatedConfigurationAsync(cancellationToken);
    }

    private async Task<Result<Unit, ScrapeError>> ProcessAllCategoryPagesAsync(SearchProgress progress, Category searchCategory, string combinedSearchString, CancellationToken cancellationToken)
    {
        long startTimestamp = timeProvider.GetTimestamp();
        logger.Debug("About to visit the specific {Category} pages now...", searchCategory.Name);

        var plan = PagedScrapePlanFactory.Create(
            progress.SearchConfiguration.StartingPageNumber,
            progress.SearchConfiguration.TotalPages,
            pageNumber => RecordCategoryPageProgress(progress, searchCategory, pageNumber),
            pageNumber => searchResultsPage.LoadSearchPageAsync(combinedSearchString, pageNumber),
            () => searchResultsPage.ImagePageLinksAsync(),
            (links, innerCt) => imagePageService.GetTheImagePagesAsync(links, searchCategory.Id, searchCategory.Name, innerCt));

        return (await pagedScrapeRunner.RunAsync(plan, cancellationToken).ConfigureAwait(false))
            .Tap(_ => logger.Information("Completed visiting the {Category}. Total time: {CategoryVisitDuration}", searchCategory.Name, timeProvider.GetElapsedTime(startTimestamp)));
    }

    private void RecordCategoryPageProgress(SearchProgress progress, Category searchCategory, int pageNumber)
    {
        logger.Debug("About to visit page {page} (of {totalPages}) for {Category} now...", pageNumber, progress.SearchConfiguration.TotalPages, searchCategory.Name);
        searchCategory.LastPageVisited = pageNumber;
    }
}
