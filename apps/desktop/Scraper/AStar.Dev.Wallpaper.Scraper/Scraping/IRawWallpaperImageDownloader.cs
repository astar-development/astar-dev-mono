using AStar.Dev.FunctionalParadigm;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Downloads a wallpaper's full-size image bytes without any decoration (e.g. thumbnail publishing).
/// </summary>
public interface IRawWallpaperImageDownloader
{
    /// <summary>
    ///     Navigates to the image URL and reads its response body.
    /// </summary>
    /// <param name="page">The Playwright page to navigate.</param>
    /// <param name="imageUrl">The full-size wallpaper image URL.</param>
    /// <param name="categoryName">The name of the search category the wallpaper was found under.</param>
    /// <param name="tags">The tags kept for the wallpaper after curation.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the download.</param>
    /// <returns>A <see cref="Failure{T}" /> when navigation fails to produce a response, or the download otherwise throws.</returns>
    Task<Exceptional<byte[]>> DownloadAsync(IPage page, string imageUrl, string categoryName, IReadOnlyList<string> tags, CancellationToken cancellationToken);
}
