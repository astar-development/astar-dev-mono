using System.Globalization;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public interface ITopWallpapersPageFunctional
{
    Task<IReadOnlyCollection<string>> GetImagePageLinks();
    Task<IResponse?> LoadTopWallpapersPageAsync(int pageNumber);
    Task<int> PageInfoAsync();
}

public sealed class TopWallpapersPageFunctional(SearchConfiguration searchConfiguration, IPlaywrightService playwrightService) : ITopWallpapersPageFunctional
{
    private IPage page = null!;

    public async Task<IResponse?> LoadTopWallpapersPageAsync(int pageNumber)
    {
        page ??= await playwrightService.ConfigurePlaywrightAsync();
        return _ = await page.GotoAsync($"{searchConfiguration.TopWallpapers}{pageNumber}");
    }

    public async Task<int> PageInfoAsync()
    {
        page ??= await playwrightService.ConfigurePlaywrightAsync();
        string? text = await page.GetByText("Page ", new PageGetByTextOptions { Exact = false, }).First.TextContentAsync();

        if (text is null) return 0;

        int firstSlashIndex = text.IndexOf('/') + 1;
        string pages = text[firstSlashIndex..].Trim();

        return int.Parse(pages, CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyCollection<string>> GetImagePageLinks()
    {
        page ??= await playwrightService.ConfigurePlaywrightAsync();
        List<string> wantedLinks = [];
        var imagePreviews = await page.GetByRole(AriaRole.Link).AllAsync();

        foreach (var imagePreview in imagePreviews)
        {
            string? hrefString = await imagePreview.GetAttributeAsync("href");

            if (hrefString != null && hrefString.Contains("/w/")) wantedLinks.Add(hrefString);
        }

        return [.. wantedLinks.Take(24)];
    }
}
