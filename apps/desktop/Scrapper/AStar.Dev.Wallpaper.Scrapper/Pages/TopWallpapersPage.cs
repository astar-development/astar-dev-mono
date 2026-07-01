using System.Globalization;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public sealed class TopWallpapersPage(IPlaywrightService playwrightService, SearchConfiguration searchConfiguration)
{
    private IPage page = null!;

    private ILocator PageCount => page.GetByText("Page ", new PageGetByTextOptions { Exact = false, });

    private ILocator ImagePreviews => page.GetByRole(AriaRole.Link);

    public async Task<IResponse?> LoadTopWallpapersPageAsync(int pageNumber)
        => _ = await page.GotoAsync($"{searchConfiguration.TopWallpapers}{pageNumber}");

    public async Task<int> PageInfoAsync()
    {
        string? text = await PageCount.First.TextContentAsync();

        if (text is null) return 0;

        int firstSlashIndex = text.IndexOf('/') + 1;
        string pages = text[firstSlashIndex..].Trim();

        return int.Parse(pages, CultureInfo.InvariantCulture);
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
}
