using System.Globalization;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping.TopWallpapers;

/// <inheritdoc cref="ITopWallpapersPageInfoReader" />
public sealed class TopWallpapersPageInfoReader : ITopWallpapersPageInfoReader
{
    /// <inheritdoc />
    public async Task<int> ReadPageCountAsync(IPage page, CancellationToken cancellationToken)
    {
        var header = page.GetByText("Page ", new PageGetByTextOptions { Exact = false, });
        string? headerText = await header.First.TextContentAsync();
        int firstSlashIndex = headerText!.IndexOf('/') + 1;
        string pages = headerText[firstSlashIndex..].Trim();

        return int.Parse(pages, CultureInfo.InvariantCulture);
    }
}
