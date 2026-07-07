using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class SubscriptionsImagesListPage(IPlaywrightService playwrightService, SearchConfiguration searchConfiguration)
{
    private const string PageHeaderErrorSource = "subscriptions-page-header";
    private const string ImagePageLinksErrorSource = "subscriptions-image-page-links";
    private const string ClearAllErrorSource = "subscriptions-clear-all";

    private IPage page = null!;

    private ILocator ImagePreviews => page.GetByRole(AriaRole.Link);

    private ILocator NewSubscriptionWallpapersHeader => page.GetByText("New Subscription Wallpapers", new PageGetByTextOptions { Exact = false, });

    public async Task<Result<Unit, ScrapeError>> LoadSubscriptionResultsPageAsync(int pageNumber)
        => (await Try.RunAsync(() => GotoSubscriptionsPageAsync(pageNumber)).ConfigureAwait(false))
            .ToResult<Unit, ScrapeError>(exception => ScrapeErrorFactory.CreatePageLoadFailed($"{searchConfiguration.Subscriptions}{pageNumber}", exception.Message));

    public async Task<Result<PageInfo, ScrapeError>> PageInfoAsync()
        => (await Try.RunAsync(GetPageHeaderAsync).ConfigureAwait(false))
            .ToResult<string?, ScrapeError>(exception => ScrapeErrorFactory.CreatePageLoadFailed(PageHeaderErrorSource, exception.Message))
            .Bind(SubscriptionHeaderParser.Parse);

    public async Task<Result<IReadOnlyCollection<string>, ScrapeError>> GetImagePageLinksAsync()
        => (await Try.RunAsync(GetTheImagePageLinksAsync).ConfigureAwait(false))
            .ToResult<IReadOnlyCollection<string>, ScrapeError>(exception => ScrapeErrorFactory.CreatePageLoadFailed(ImagePageLinksErrorSource, exception.Message));

    public async Task<Result<Unit, ScrapeError>> ClearAsync()
        => (await Try.RunAsync(ClickClearAllSubscriptionsAsync).ConfigureAwait(false))
            .ToResult<Unit, ScrapeError>(exception => ScrapeErrorFactory.CreatePageLoadFailed(ClearAllErrorSource, exception.Message));

    private async Task<Unit> GotoSubscriptionsPageAsync(int pageNumber)
    {
        await EnsurePageAsync().ConfigureAwait(false);
        _ = await page.GotoAsync($"{searchConfiguration.Subscriptions}{pageNumber}").ConfigureAwait(false);

        return Unit.Value;
    }

    private async Task<string?> GetPageHeaderAsync()
    {
        await EnsurePageAsync().ConfigureAwait(false);

        return await NewSubscriptionWallpapersHeader.TextContentAsync().ConfigureAwait(false);
    }

    private async Task<IReadOnlyCollection<string>> GetTheImagePageLinksAsync()
    {
        await EnsurePageAsync().ConfigureAwait(false);

        var imagePreviews = await ImagePreviews.AllAsync().ConfigureAwait(false);
        List<string?> hrefs = [];
        foreach (var imagePreview in imagePreviews) hrefs.Add(await imagePreview.GetAttributeAsync("href").ConfigureAwait(false));

        return ImageLinkSelector.SelectWanted(hrefs);
    }

    private async Task<Unit> ClickClearAllSubscriptionsAsync()
    {
        await EnsurePageAsync().ConfigureAwait(false);

        await page.Locator("div")
                  .Filter(new LocatorFilterOptions { HasText = " Clear All Subscriptions", })
                  .GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = " Clear All Subscriptions", })
                  .ClickAsync().ConfigureAwait(false);

        return Unit.Value;
    }

    private async Task EnsurePageAsync() => page ??= await playwrightService.ConfigurePlaywrightAsync().ConfigureAwait(false);
}
