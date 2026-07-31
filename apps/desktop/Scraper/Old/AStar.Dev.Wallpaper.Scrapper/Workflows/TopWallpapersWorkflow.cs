using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

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

    private SearchConfiguration searchConfiguration = scrapeConfiguration.SearchConfiguration;

    public Task<Result<Unit, ScrapeError>> RunAsync(CancellationToken ct = default)
        => RunTopWallpapersAsync(ct).LogFailure(logger);

    private async Task<Result<Unit, ScrapeError>> RunTopWallpapersAsync(CancellationToken ct)
    {
        await LoadStartingPageAsync().ConfigureAwait(false);

        return await topWallpapersPage.PageInfoAsync()
            .BindAsync(pageCount => ProcessTopWallpapersAsync(pageCount, ct))
            .ConfigureAwait(false);
    }

    private async Task LoadStartingPageAsync()
    {
        var loadResult = await topWallpapersPage.LoadTopWallpapersPageAsync(searchConfiguration.TopWallpapersStartingPageNumber).ConfigureAwait(false);
        var loadedSuccessfully = loadResult.Match(_ => true, _ => false);

        if (!loadedSuccessfully) _ = await topWallpapersPage.LoadTopWallpapersPageAsync(FirstPageNumber).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ScrapeError>> ProcessTopWallpapersAsync(int pageCount, CancellationToken ct)
    {
        logger.Information("There are a total of {TopWallpapersPageCount} pages for the Top Wallpapers.", pageCount);

        if (searchConfiguration.TopWallpapersTotalPages != pageCount) searchConfiguration = searchConfiguration with { TopWallpapersTotalPages = pageCount, };

        await configurationSaver.SaveUpdatedConfigurationAsync().ConfigureAwait(false);

        var plan = PagedScrapePlanFactory.Create(
            searchConfiguration.TopWallpapersStartingPageNumber,
            searchConfiguration.TopWallpapersTotalPages,
            _ => { },
            LoadTopWallpapersPageAsync,
            topWallpapersPage.GetImagePageLinksAsync,
            (links, innerCt) => imagePageService.GetTheImagePagesAsync(links, NoCategory, NoCategory, innerCt));

        return await pagedScrapeRunner.RunAsync(plan, ct).ConfigureAwait(false);
    }

    private async Task<Result<Unit, ScrapeError>> LoadTopWallpapersPageAsync(int pageNumber)
    {
        _ = await topWallpapersPage.LoadTopWallpapersPageAsync(pageNumber).ConfigureAwait(false);

        return Unit.Value;
    }
}
