using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Pages;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Support;
using Serilog;

namespace AStar.Dev.Wallpaper.Scraper.Workflows;

public sealed class TopWallpapersWorkflow(
    ITopWallpapersPage topWallpapersPage,
    ImagePageService imagePageService,
    ScrapeConfiguration scrapeConfiguration,
    ConfigurationSaver configurationSaver,
    PagedScrapeRunner pagedScrapeRunner,
    ILogger logger)
{
    private const int FirstPageNumber = 1;
    private const string NoCategory = "";

    public Task<Result<Unit, ScrapeError>> RunAsync(CancellationToken cancellationToken = default)
        => RunTopWallpapersAsync(cancellationToken).LogFailure(logger);

    private async Task<Result<Unit, ScrapeError>> RunTopWallpapersAsync(CancellationToken cancellationToken)
    {
        var searchConfiguration = scrapeConfiguration.SearchConfiguration;

        await LoadStartingPageAsync(searchConfiguration.TopWallpapersStartingPageNumber).ConfigureAwait(false);

        return await topWallpapersPage.PageInfoAsync()
            .BindAsync(pageCount => ProcessTopWallpapersAsync(searchConfiguration with { TopWallpapersTotalPages = pageCount, }, cancellationToken))
            .ConfigureAwait(false);
    }

    private async Task LoadStartingPageAsync(int startingPageNumber)
    {
        var loadResult = await topWallpapersPage.LoadTopWallpapersPageAsync(startingPageNumber).ConfigureAwait(false);
        bool loadedSuccessfully = loadResult.Match(_ => true, _ => false);

        if (!loadedSuccessfully) _ = await topWallpapersPage.LoadTopWallpapersPageAsync(FirstPageNumber).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ScrapeError>> ProcessTopWallpapersAsync(SearchConfiguration searchConfiguration, CancellationToken cancellationToken)
    {
        logger.Information("There are a total of {TopWallpapersPageCount} pages for the Top Wallpapers.", searchConfiguration.TopWallpapersTotalPages);

        await configurationSaver.SaveUpdatedConfigurationAsync().ConfigureAwait(false);

        var plan = PagedScrapePlanFactory.Create(
            searchConfiguration.TopWallpapersStartingPageNumber,
            searchConfiguration.TopWallpapersTotalPages,
            _ => { },
            LoadTopWallpapersPageAsync,
            topWallpapersPage.GetImagePageLinksAsync,
            (links, innerCt) => imagePageService.GetTheImagePagesAsync(links, NoCategory, NoCategory, innerCt));

        return await pagedScrapeRunner.RunAsync(plan, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ScrapeError>> LoadTopWallpapersPageAsync(int pageNumber)
    {
        _ = await topWallpapersPage.LoadTopWallpapersPageAsync(pageNumber).ConfigureAwait(false);

        return Unit.Value;
    }
}
