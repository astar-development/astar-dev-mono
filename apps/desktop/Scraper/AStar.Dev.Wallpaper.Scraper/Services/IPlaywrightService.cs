using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Wallpaper.Scraper.Services;

/// <summary>
///     Defines a service for configuring and managing Playwright instances.
/// </summary>
public interface IPlaywrightService
{
    /// <summary>
    ///     Configures and returns a Playwright instance.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the configured Playwright <see cref="Microsoft.Playwright.IPage"/> instance.
    /// </returns>
    Task<Exceptional<Microsoft.Playwright.IPage>> ConfigurePlaywrightAsync(CancellationToken cancellationToken);
}
