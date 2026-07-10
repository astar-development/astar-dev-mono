using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Support;
using Microsoft.Playwright;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public class PlaywrightService(ScrapeConfiguration scrapeConfiguration, TimeProvider clock, Logger logger) : IPlaywrightService, IAsyncDisposable
{
    private IPlaywright? playwright;
    private IBrowserContext? context;
    private IPage? page;

    public async Task<IPage> ConfigurePlaywrightAsync()
    {
        if (page is not null)
        {
            return page;
        }

        playwright ??= await Playwright.CreateAsync();

        context = await playwright.Chromium.LaunchPersistentContextAsync(
            "/home/jbarden/.config/google-chrome/Default",
            new BrowserTypeLaunchPersistentContextOptions
            {
                Channel = "chrome",
                Headless = false,
                BaseURL = scrapeConfiguration.SearchConfiguration.BaseUrl,
                ViewportSize = new ViewportSize { Width = 3000, Height = 1200 },
                Locale = "en-GB",
                TimezoneId = "Europe/London",
            });

        await ApplyCookiesAsync(context, logger, scrapeConfiguration);

        page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        return page;
    }

    public async ValueTask DisposeAsync()
    {
        if (page is not null)
        {
            await page.CloseAsync();
            page = null;
        }

        if (context is not null)
        {
            await context.CloseAsync();
            context = null;
        }

        playwright?.Dispose();
        playwright = null;

        GC.SuppressFinalize(this);
    }

    private async Task ApplyCookiesAsync(IBrowserContext context, Logger logger, ScrapeConfiguration scrapeConfiguration)
    {
        string domain = ExtractDomain(scrapeConfiguration.SearchConfiguration.BaseUrl);

        var chromeCookies = await ChromeCookieExtractor.ExtractAsync(domain, scrapeConfiguration.UserConfiguration.SessionCookie, clock);

        chromeCookies.ForEach(async cookie => await context.AddCookiesAsync([cookie]));
    }

    private static string ExtractDomain(string baseUrl)
    {
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }

        return baseUrl.TrimStart('.');
    }
}
