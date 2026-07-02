using System.Globalization;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class SubscriptionsImagesListPage(IPlaywrightService playwrightService, SearchConfiguration searchConfiguration)
{
    private IPage page = null!;

    private ILocator ImagePreviews => page.GetByRole(AriaRole.Link);

    private ILocator NewSubscriptionWallpapersHeader => page.GetByText("New Subscription Wallpapers", new PageGetByTextOptions { Exact = false, });

    public async Task<IResponse?> LoadSubscriptionResultsPageAsync(int pageNumber)
    {
        page ??= await playwrightService.ConfigurePlaywrightAsync();
        return await page.GotoAsync($"{searchConfiguration.Subscriptions}{pageNumber}");
    }

    public async Task<(int pageCount, string subDirectoryName)> PageInfoAsync()
    {
        page ??= await playwrightService.ConfigurePlaywrightAsync();
        string? text = await NewSubscriptionWallpapersHeader.TextContentAsync();

        if (text is null) return (0, string.Empty);

        int firstSpaceIndex = text.IndexOf(' ');
        int hashIndex = text.IndexOf("New", StringComparison.Ordinal);
        string subDirectoryName = string.Empty;

        if (hashIndex > 0) subDirectoryName = text[hashIndex..].Replace(" ", "-").Replace("#", string.Empty);

        string searchResults = text.Replace(",", string.Empty)[..firstSpaceIndex];
        decimal imageCount = decimal.Parse(searchResults, CultureInfo.InvariantCulture) / 24;

        return (Convert.ToInt32(Math.Ceiling(imageCount)), subDirectoryName);
    }

    public async Task<IReadOnlyCollection<string>> GetImagePageLinks()
    {
        page ??= await playwrightService.ConfigurePlaywrightAsync();
        List<string> wantedLinks = [];
        var imagePreviews = await ImagePreviews.AllAsync();

        foreach (var imagePreview in imagePreviews)
        {
            string? hrefString = await imagePreview.GetAttributeAsync("href");

            if (hrefString != null && hrefString.Contains("/w/")) wantedLinks.Add(hrefString);
        }

        return [.. wantedLinks.Take(24)];
    }

    public async Task Clear()
        => await page.Locator("div")
                     .Filter(new LocatorFilterOptions { HasText = " Clear All Subscriptions", })
                     .GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = " Clear All Subscriptions", })
                     .ClickAsync();
}
