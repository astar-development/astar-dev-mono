using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

public sealed class SubscriptionsWorkflow(
    SubscriptionsImagesListPage subscriptionsImagesListPage,
    ImagePageService imagePageService,
    ScrapeConfiguration scrapeConfiguration,
    ConfigurationSaver configurationSaver,
    PagedScrapeRunner pagedScrapeRunner,
    ILogger logger)
{
    private const int FirstPageNumber = 1;

    private SearchConfiguration searchConfiguration = scrapeConfiguration.SearchConfiguration;
    private ScrapeDirectories scrapeDirectories = scrapeConfiguration.ScrapeDirectories;

    public Task<Result<Unit, ScrapeError>> RunAsync(CancellationToken ct = default)
        => RunSubscriptionsAsync(ct).LogFailure(logger);

    private async Task<Result<Unit, ScrapeError>> RunSubscriptionsAsync(CancellationToken ct)
    {
        searchConfiguration = searchConfiguration with { SubscriptionsStartingPageNumber = FirstPageNumber, };

        await LoadStartingPageAsync().ConfigureAwait(false);

        return await subscriptionsImagesListPage.PageInfoAsync()
            .BindAsync(pageInfo => ProcessSubscriptionsAsync(pageInfo, ct))
            .ConfigureAwait(false);
    }

    private async Task LoadStartingPageAsync()
    {
        var loadResult = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(FirstPageNumber).ConfigureAwait(false);
        var loadedSuccessfully = loadResult.Match(_ => true, _ => false);

        if (!loadedSuccessfully) _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(FirstPageNumber).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ScrapeError>> ProcessSubscriptionsAsync(PageInfo pageInfo, CancellationToken ct)
    {
        if (pageInfo.SubDirectoryName.Length > 0) scrapeDirectories = scrapeDirectories with { SubDirectoryName = pageInfo.SubDirectoryName, };

        if (searchConfiguration.SubscriptionsTotalPages != pageInfo.PageCount) searchConfiguration = searchConfiguration with { SubscriptionsTotalPages = pageInfo.PageCount, };

        await configurationSaver.SaveUpdatedConfigurationAsync().ConfigureAwait(false);

        var plan = PagedScrapePlanFactory.Create(
            searchConfiguration.SubscriptionsStartingPageNumber,
            searchConfiguration.SubscriptionsTotalPages,
            _ => { },
            pageNumber => LoadSubscriptionsPageAsync(pageNumber, searchConfiguration.SubscriptionsTotalPages),
            subscriptionsImagesListPage.GetImagePageLinksAsync,
            (links, innerCt) => imagePageService.GetTheImagePagesAsync(links, string.Empty, pageInfo.SubDirectoryName, innerCt));

        return await pagedScrapeRunner.RunAsync(plan, ct)
            .BindAsync(_ => ClearSubscriptionsIfCompleteAsync(pageInfo))
            .ConfigureAwait(false);
    }

    private async Task<Result<Unit, ScrapeError>> LoadSubscriptionsPageAsync(int pageNumber, int totalPages)
    {
        logger.Information("Getting page {subscriptionPage} (of {totalPagesForSubscriptions}) now.", pageNumber, totalPages);
        _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(pageNumber).ConfigureAwait(false);

        return Unit.Value;
    }

    private async Task<Result<Unit, ScrapeError>> ClearSubscriptionsIfCompleteAsync(PageInfo pageInfo)
    {
        if (pageInfo.PageCount <= 0) return Unit.Value;

        _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(FirstPageNumber).ConfigureAwait(false);
        _ = await subscriptionsImagesListPage.ClearAsync().ConfigureAwait(false);

        return Unit.Value;
    }
}
