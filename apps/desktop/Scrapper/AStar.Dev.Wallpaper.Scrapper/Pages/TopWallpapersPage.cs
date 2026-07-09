using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class TopWallpapersPage(IPlaywrightService playwrightService, SearchConfiguration searchConfiguration) : ITopWallpapersPage
{
    private const string PageHeaderErrorSource = "top-wallpapers-page-header";
    private const string ImagePageLinksErrorSource = "top-wallpapers-image-page-links";

    private IPage page = null!;

    private ILocator PageCount => page.GetByText("Page ", new PageGetByTextOptions { Exact = false, });

    private ILocator ImagePreviews => page.GetByRole(AriaRole.Link);

    public async Task<Result<Unit, ScrapeError>> LoadTopWallpapersPageAsync(int pageNumber)
        => (await Try.RunAsync(() => GotoTopWallpapersPageAsync(pageNumber)).ConfigureAwait(false))
            .ToResult<Unit, ScrapeError>(exception => ScrapeErrorFactory.CreatePageLoadFailed($"{searchConfiguration.TopWallpapers}{pageNumber}", exception.Message));

    public async Task<Result<int, ScrapeError>> PageInfoAsync()
        => (await Try.RunAsync(GetPageHeaderAsync).ConfigureAwait(false))
            .ToResult<string?, ScrapeError>(exception => ScrapeErrorFactory.CreatePageLoadFailed(PageHeaderErrorSource, exception.Message))
            .Bind(TopWallpapersHeaderParser.Parse);

    public async Task<Result<IReadOnlyCollection<string>, ScrapeError>> GetImagePageLinksAsync()
        => (await Try.RunAsync(GetTheImagePageLinksAsync).ConfigureAwait(false))
            .ToResult<IReadOnlyCollection<string>, ScrapeError>(exception => ScrapeErrorFactory.CreatePageLoadFailed(ImagePageLinksErrorSource, exception.Message));

    private async Task<Unit> GotoTopWallpapersPageAsync(int pageNumber)
    {
        await EnsurePageAsync().ConfigureAwait(false);
        _ = await page.GotoAsync($"{searchConfiguration.TopWallpapers}{pageNumber}").ConfigureAwait(false);

        return Unit.Value;
    }

    private async Task<string?> GetPageHeaderAsync()
    {
        await EnsurePageAsync().ConfigureAwait(false);

        return await PageCount.First.TextContentAsync().ConfigureAwait(false);
    }

    private async Task<IReadOnlyCollection<string>> GetTheImagePageLinksAsync()
    {
        await EnsurePageAsync().ConfigureAwait(false);

        var imagePreviews = await ImagePreviews.AllAsync().ConfigureAwait(false);
        List<string?> hrefs = [];
        foreach (var imagePreview in imagePreviews) hrefs.Add(await imagePreview.GetAttributeAsync("href").ConfigureAwait(false));

        return ImageLinkSelector.SelectWanted(hrefs);
    }

    private async Task EnsurePageAsync() => page ??= await playwrightService.ConfigurePlaywrightAsync().ConfigureAwait(false);
}
