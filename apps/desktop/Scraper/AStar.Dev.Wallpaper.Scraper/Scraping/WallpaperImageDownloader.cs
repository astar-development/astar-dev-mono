using AStar.Dev.FunctionalParadigm;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Downloads a wallpaper's full-size image bytes.
/// </summary>
public sealed class WallpaperImageDownloader : IRawWallpaperImageDownloader
{
    /// <inheritdoc />
    public async Task<Exceptional<byte[]>> DownloadAsync(IPage page, string imageUrl, string categoryName, IReadOnlyList<string> tags, CancellationToken cancellationToken) =>
        await Try.RunAsync(async () =>
        {
            var response = await page.APIRequest.GetAsync(imageUrl).ConfigureAwait(false);

            if (!response.Ok)
            {
                throw new InvalidOperationException($"Requesting '{imageUrl}' returned status {response.Status}.");
            }

            return await response.BodyAsync().ConfigureAwait(false);
        }, cancellationToken);
}
