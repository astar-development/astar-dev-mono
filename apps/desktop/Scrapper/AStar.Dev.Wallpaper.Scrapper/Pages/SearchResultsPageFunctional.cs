using System.Globalization;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Playwright;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class SearchResultsPageFunctional(IPlaywrightService playwrightService, Logger logger)
{
    private IPage page = null!;

    private ILocator NewSubscriptionWallpapersHeader => page.GetByText("New Subscription Wallpapers", new PageGetByTextOptions { Exact = false, });

    private ILocator WallpaperSearchHeader => page.GetByText("Wallpapers found for", new PageGetByTextOptions { Exact = false, });

    private ILocator WallpaperSearchHeaderGeneral => page.GetByText("Wallpapers found", new PageGetByTextOptions { Exact = false, });

    private ILocator ImagePreviews => page.GetByRole(AriaRole.Link);

    public async Task<IResponse?> LoadSearchPageAsync(string searchString, int pageNumber)
    {
        try
        {
            page ??= await playwrightService.ConfigurePlaywrightAsync();
            return await GotoSearchPageAsync(searchString, pageNumber);
        }
        catch (Exception exception)
        {
            logger.Error(exception.GetBaseException().Message);

            throw;
        }
    }

    public async Task<(int pageCount, int imageCount, string subDirectoryName)> PageInfoAsync()
    {
        try
        {
            page ??= await playwrightService.ConfigurePlaywrightAsync();
            return await GetPageInfoAsync();
        }
        catch (Exception exception)
        {
            logger.Error(exception.GetBaseException().Message);

            throw;
        }
    }

    public async Task<IReadOnlyCollection<string>> ImagePageLinksAsync()
    {
        try
        {
            page ??= await playwrightService.ConfigurePlaywrightAsync();
            return await GetTheImagePageLinksAsync();
        }
        catch (Exception exception)
        {
            logger.Error(exception.GetBaseException().Message);

            throw;
        }
    }

    private async Task<IResponse?> GotoSearchPageAsync(string searchString, int pageNumber)
    {
        var searchPage = await GotoPageAsync(searchString, pageNumber);

        return searchPage is { Ok: true, } ? searchPage : await GotoPageAsync(searchString, pageNumber);
    }

    private async Task<IReadOnlyCollection<string>> GetTheImagePageLinksAsync()
    {
        List<string> wantedLinks = [];
        var imagePreviews = await ImagePreviews.AllAsync();

        foreach (var imagePreview in imagePreviews)
        {
            string? hrefString = await imagePreview.GetAttributeAsync("href");

            if (hrefString != null && hrefString.Contains("/w/")) wantedLinks.Add(hrefString);
        }

        return [.. wantedLinks.Take(24)];
    }

    private async Task<(int pageCount, int imageCount, string subDirectoryName)> GetPageInfoAsync()
    {
        string? text = await GetPageHeader();

        if (text is null) return (0, 0, string.Empty);

        int firstSpaceIndex = text.IndexOf(' ');
        int hashIndex = text.IndexOf("for ", StringComparison.Ordinal) + 3;
        string subDirectoryName = string.Empty;

        if (hashIndex > 0) subDirectoryName = text[(hashIndex + 1)..].Replace(" ", "-").Replace("#", string.Empty);

        string searchResults = text.Replace(",", string.Empty)[..firstSpaceIndex];
        decimal imageCount = decimal.Parse(searchResults, CultureInfo.InvariantCulture);

        return (Convert.ToInt32(Math.Ceiling(imageCount / 24)), (int)imageCount, subDirectoryName);
    }

    private async Task<IResponse?> GotoPageAsync(string searchString, int pageNumber)
        => await page.GotoAsync($"{searchString}{pageNumber}", new PageGotoOptions { Timeout = 60000, });

    private async Task<string?> GetPageHeader()
    {
        string? text;

        try
        {
            text = await WallpaperSearchHeader.TextContentAsync();
        }
        catch
        {
            text = await WallpaperSearchHeaderGeneral.TextContentAsync();
        }

        if (text?.Length == 0) text = await NewSubscriptionWallpapersHeader.TextContentAsync();

        return text;
    }
}
