using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Playwright;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

public sealed class SubscriptionsWorkflow(
    SubscriptionsImagesListPage subscriptionsImagesListPage,
    ImagePageService            imagePageService,
    SearchConfiguration         searchConfiguration,
    ScrapeDirectories           scrapeDirectories,
    ConfigurationSaver          configurationSaver,
    Logger                      logger)
{
    private SearchConfiguration _searchConfiguration = searchConfiguration;
    private ScrapeDirectories   _scrapeDirectories   = scrapeDirectories;

    public async Task RunAsync(CancellationToken ct = default)
    {
        try
        {
            await GetTheNewSubscriptionImagesAsync(ct);
        }
        catch(Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(exception.GetBaseException().Message);
            throw;
        }
    }

    private async Task GetTheNewSubscriptionImagesAsync(CancellationToken ct)
    {
        _searchConfiguration = _searchConfiguration with { SubscriptionsStartingPageNumber = 1 };
        IResponse? pageDetails = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(_searchConfiguration.SubscriptionsStartingPageNumber);

        if(pageDetails is { Ok: false, }) _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(1);

        var (pageCount, subDirectoryName) = await subscriptionsImagesListPage.PageInfoAsync();

        if(subDirectoryName.Length > 0) _scrapeDirectories = _scrapeDirectories with { SubDirectoryName = subDirectoryName };

        UpdateSearchTotalPagesIfRequired(pageCount);

        await configurationSaver.SaveUpdatedConfigurationAsync();

        for(var currentPageNumber = _searchConfiguration.SubscriptionsStartingPageNumber;
            currentPageNumber <= _searchConfiguration.SubscriptionsTotalPages;
            currentPageNumber++)
        {
            ct.ThrowIfCancellationRequested();
            var delay = Random.Shared.Next(_searchConfiguration.ImagePauseInSeconds, _searchConfiguration.ImagePauseInSeconds + 4);
            await Task.Delay(TimeSpan.FromSeconds(delay), ct);
            _searchConfiguration = _searchConfiguration with { SubscriptionsStartingPageNumber = currentPageNumber };
            await configurationSaver.SaveUpdatedConfigurationAsync();
            logger.Information("Getting page {subscriptionPage} (of {totalPagesForSubscriptions}) now.", currentPageNumber, _searchConfiguration.SubscriptionsTotalPages);
            _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(currentPageNumber);
            IReadOnlyCollection<string> imagePageLinks = await subscriptionsImagesListPage.GetImagePageLinks();

            await imagePageService.GetTheImagePagesAsync(imagePageLinks, "", subDirectoryName, ct: ct);
        }

        if(pageCount > 0)
        {
            _ = await subscriptionsImagesListPage.LoadSubscriptionResultsPageAsync(1);
            await subscriptionsImagesListPage.Clear();
        }
    }

    private void UpdateSearchTotalPagesIfRequired(int pageCount)
    {
        if(_searchConfiguration.SubscriptionsTotalPages != pageCount) _searchConfiguration = _searchConfiguration with { SubscriptionsTotalPages = pageCount };
    }
}
