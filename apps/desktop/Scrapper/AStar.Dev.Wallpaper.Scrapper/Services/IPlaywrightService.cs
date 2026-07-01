using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public interface IPlaywrightService
{
    /// <summary>
    /// Configures and initializes a Playwright browser context and page for web scraping. This method sets up the browser with specified options, including headless mode, slow motion delay, and user agent settings. It also applies cookies extracted from the Chrome browser to maintain session state and authentication. The method returns an initialized IPage instance that can be used for navigating and interacting with web pages during the scraping process.
    /// </summary>
    /// <returns>The initialized IPage instance.</returns>
    Task<IPage> ConfigurePlaywrightAsync();
}