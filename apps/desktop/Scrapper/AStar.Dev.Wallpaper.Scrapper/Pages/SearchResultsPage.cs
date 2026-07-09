using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class SearchResultsPage(IPlaywrightService playwrightService)
{
    private const string PageHeaderErrorSource = "search-results-page-header";
    private const string ImagePageLinksErrorSource = "search-results-image-page-links";

    private IPage page = null!;

    private ILocator NewSubscriptionWallpapersHeader => page.GetByText("New Subscription Wallpapers", new PageGetByTextOptions { Exact = false, });

    private ILocator WallpaperSearchHeader => page.GetByText("Wallpapers found for", new PageGetByTextOptions { Exact = false, });

    private ILocator WallpaperSearchHeaderGeneral => page.GetByText("Wallpapers found", new PageGetByTextOptions { Exact = false, });

    private ILocator ImagePreviews => page.GetByRole(AriaRole.Link);

    public async Task<Result<Unit, ScrapeError>> LoadSearchPageAsync(string searchString, int pageNumber)
        => (await Try.RunAsync(() => AttemptGotoSearchPageAsync(searchString, pageNumber)).ConfigureAwait(false))
            .ToResult<bool, ScrapeError>(exception => ScrapeErrorFactory.CreatePageLoadFailed($"{searchString}{pageNumber}", exception.Message))
            .Bind(succeeded => ToSearchPageResult(succeeded, searchString, pageNumber));

    public async Task<Result<PageInfo, ScrapeError>> PageInfoAsync()
        => (await Try.RunAsync(GetPageHeaderAsync).ConfigureAwait(false))
            .ToResult<string?, ScrapeError>(exception => ScrapeErrorFactory.CreatePageLoadFailed(PageHeaderErrorSource, exception.Message))
            .Bind(PageHeaderParser.Parse);

    public async Task<Result<IReadOnlyCollection<string>, ScrapeError>> ImagePageLinksAsync()
        => (await Try.RunAsync(GetTheImagePageLinksAsync).ConfigureAwait(false))
            .ToResult<IReadOnlyCollection<string>, ScrapeError>(exception => ScrapeErrorFactory.CreatePageLoadFailed(ImagePageLinksErrorSource, exception.Message));

    private static Result<Unit, ScrapeError> ToSearchPageResult(bool succeeded, string searchString, int pageNumber)
    {
        if (succeeded) return Unit.Value;

        return ScrapeErrorFactory.CreatePageLoadFailed($"{searchString}{pageNumber}", $"Could not load the search results page for {searchString}{pageNumber} after retry.");
    }

    private async Task<bool> AttemptGotoSearchPageAsync(string searchString, int pageNumber)
    {
        await EnsurePageAsync().ConfigureAwait(false);

        var firstAttempt = await GotoPageAsync(searchString, pageNumber).ConfigureAwait(false);

        if (firstAttempt is { Ok: true, }) return true;

        var secondAttempt = await GotoPageAsync(searchString, pageNumber).ConfigureAwait(false);

        return secondAttempt is { Ok: true, };
    }

    private async Task<IReadOnlyCollection<string>> GetTheImagePageLinksAsync()
    {
        await EnsurePageAsync().ConfigureAwait(false);

        var imagePreviews = await ImagePreviews.AllAsync().ConfigureAwait(false);
        List<string?> hrefs = [];
        foreach (var imagePreview in imagePreviews) hrefs.Add(await imagePreview.GetAttributeAsync("href").ConfigureAwait(false));

        return ImageLinkSelector.SelectWanted(hrefs);
    }

    private async Task<IResponse?> GotoPageAsync(string searchString, int pageNumber)
        => await page.GotoAsync($"{searchString}{pageNumber}", new PageGotoOptions { Timeout = 60000, }).ConfigureAwait(false);

    private async Task<string?> GetPageHeaderAsync()
    {
        await EnsurePageAsync().ConfigureAwait(false);

        string? text;

        try
        {
            text = await WallpaperSearchHeader.TextContentAsync().ConfigureAwait(false);
        }
        catch
        {
            text = await WallpaperSearchHeaderGeneral.TextContentAsync().ConfigureAwait(false);
        }

        if (text?.Length == 0) text = await NewSubscriptionWallpapersHeader.TextContentAsync().ConfigureAwait(false);

        return text;
    }

    private async Task EnsurePageAsync() => page ??= await playwrightService.ConfigurePlaywrightAsync().ConfigureAwait(false);
}
