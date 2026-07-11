using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Pages;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Support;
using Serilog;

namespace AStar.Dev.Wallpaper.Scraper.Workflows;

public sealed class SubscriptionsWorkflow(
    SubscriptionsImagesListPage subscriptionsImagesListPage,
    ImagePageService imagePageService,
    ConfigurationSaver configurationSaver,
    PagedScrapeRunner pagedScrapeRunner,
    ILogger logger)
{
    private const int FirstPageNumber = 1;

    public Task<Result<Unit, ScrapeError>> RunAsync(CancellationToken cancellationToken = default)
        => RunSubscriptionsAsync(cancellationToken).LogFailure(logger);

    private async Task<Result<Unit, ScrapeError>> RunSubscriptionsAsync(CancellationToken cancellationToken)
    {
        await LoadStartingPageAsync().ConfigureAwait(false);

        return await subscriptionsImagesListPage.PageInfoAsync()
            .BindAsync(pageInfo => ProcessSubscriptionsAsync(pageInfo, cancellationToken))
            .ConfigureAwait(false);
    }

    private async Task LoadStartingPageAsync()
        => _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(FirstPageNumber)
            .OrElseAsync(_ => subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(FirstPageNumber))
            .ConfigureAwait(false);

    private async Task<Result<Unit, ScrapeError>> ProcessSubscriptionsAsync(PageInfo pageInfo, CancellationToken cancellationToken)
    {
        await configurationSaver.SaveUpdatedConfigurationAsync().ConfigureAwait(false);

        var plan = PagedScrapePlanFactory.Create(
            FirstPageNumber,
            pageInfo.PageCount,
            _ => { },
            pageNumber => LoadSubscriptionsPageAsync(pageNumber, pageInfo.PageCount),
            subscriptionsImagesListPage.GetImagePageLinksAsync,
            (links, innerCt) => imagePageService.GetTheImagePagesAsync(links, string.Empty, pageInfo.SubDirectoryName, innerCt));

        return await pagedScrapeRunner.RunAsync(plan, cancellationToken)
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
