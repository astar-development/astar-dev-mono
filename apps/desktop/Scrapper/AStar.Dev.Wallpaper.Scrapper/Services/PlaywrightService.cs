using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Microsoft.Playwright;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public class PlaywrightService(ScrapeConfiguration scrapeConfiguration, Logger logger) : IPlaywrightService, IAsyncDisposable
{
    private IPlaywright? playwright;
    private IBrowser? browser;
    private IBrowserContext? context;
    private IPage? page;

    public async Task<IPage> ConfigurePlaywrightAsync()
    {
        if (page is not null)
        {
            return page;
        }

        playwright ??= await Playwright.CreateAsync();

        browser ??= await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            SlowMo = scrapeConfiguration.SearchConfiguration.SlowMotionDelay,
            Channel = "chrome",
            Args = ["--disable-blink-features=AutomationControlled"],
        });

        context ??= await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = scrapeConfiguration.SearchConfiguration.BaseUrl,
            ViewportSize = new ViewportSize { Width = 2440, Height = 1200 },
            Locale = "en-GB",
            TimezoneId = "Europe/London",
        });

        await ApplyCookiesAsync(context, logger);

        page = await context.NewPageAsync();
        page.SetDefaultTimeout(60_000);

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

        if (browser is not null)
        {
            await browser.CloseAsync();
            browser = null;
        }

        playwright?.Dispose();
        playwright = null;

        GC.SuppressFinalize(this);
    }

    private static async Task ApplyCookiesAsync(IBrowserContext context, Logger logger)
    {
        var chromeCookies = await ChromeCookieExtractor.ExtractAsync("wallhaven.cc", null);
        logger.Information("Extracted {Count} cookies from Chrome profile", chromeCookies.Count);
        int injected = 0;
        foreach (var cookie in chromeCookies)
        {
            try
            {
                await context.AddCookiesAsync([cookie]);
                injected++;
            }
            catch (Exception ex)
            {
                logger.Debug("Skipped cookie '{Name}' ({Domain}): {Message}", cookie.Name, cookie.Domain, ex.Message);
            }
        }

        logger.Information("Injected {Injected}/{Total} cookies", injected, chromeCookies.Count);
    }
}
