using System.IO.Abstractions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Services;

/// <inheritdoc />
public class PlaywrightService(ILogger<PlaywrightService> logger, IOptions<ScrapeConfiguration> scrapeConfiguration, IFileSystem fileSystem) : IPlaywrightService, IAsyncDisposable
{
    private readonly SemaphoreSlim configureLock = new(1, 1);
    private IPlaywright? playwright;
    private IBrowserContext? context;
    private IPage? page;

    /// <inheritdoc />
    public async Task<Exceptional<IPage>> ConfigurePlaywrightAsync(CancellationToken cancellationToken)
    {
        if (page is not null) return Exceptional.Success(page);

        using var lockScope = await AcquireConfigureLockAsync(cancellationToken).ConfigureAwait(false);

        return await Try.RunAsync(CreateUserDataDirectoryAsync)
            .TapAsync(_ => LogMessage.Information(logger, "User data directory created successfully."))
            .MapAsync(_ => GetOrCreatePlaywrightAsync())
            .MapAsync(GetOrCreateContextAsync)
            .MapAsync(HideWebdriverAsync)
            .TapAsync(_ => LogMessage.Information(logger, "Playwright configured successfully."))
            .MapAsync(GetOrCreatePageAsync);
    }

    private async Task<IDisposable> AcquireConfigureLockAsync(CancellationToken cancellationToken)
    {
        await configureLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        return new ConfigureLockScope(configureLock);
    }

    private Task<IPage?> CreateUserDataDirectoryAsync()
    {
        fileSystem.Directory.CreateDirectory(scrapeConfiguration.Value.UserDataDirectory);

        return Task.FromResult<IPage?>(null);
    }

    private async Task<IPlaywright> GetOrCreatePlaywrightAsync()
        => playwright ??= await Playwright.CreateAsync().ConfigureAwait(false);

    private async Task<IBrowserContext> GetOrCreateContextAsync(IPlaywright _)
        => context ??= await playwright!.Chromium.LaunchPersistentContextAsync(scrapeConfiguration.Value.UserDataDirectory, SetContext()).ConfigureAwait(false);

    private async Task<IBrowserContext> HideWebdriverAsync(IBrowserContext browserContext)
    {
        await browserContext.AddInitScriptAsync(HideWebdriverScript).ConfigureAwait(false);

        return browserContext;
    }

    private async Task<IPage> GetOrCreatePageAsync(IBrowserContext browserContext)
        => page ??= await browserContext.NewPageAsync().ConfigureAwait(false);

    private sealed class ConfigureLockScope(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }

    private const string HideWebdriverScript = "Object.defineProperty(navigator, 'webdriver', { get: () => undefined });";

    private BrowserTypeLaunchPersistentContextOptions SetContext() => new()
    {
        BaseURL = scrapeConfiguration.Value.SearchConfiguration.BaseUrl.EnsureTrailingSlash(),
        Channel = "chrome",
        Headless = scrapeConfiguration.Value.SearchConfiguration.UseHeadless,
        Args = ["--disable-blink-features=AutomationControlled", "--password-store=kwallet6"],
        ViewportSize = new ViewportSize { Width = 2000, Height = 1200 },
        Locale = "en-GB",
        TimezoneId = "Europe/London",
    };

    public async ValueTask DisposeAsync()
    {
        var disposalExceptions = new List<Exception>();

        await CloseContextAsync(disposalExceptions).ConfigureAwait(false);
        DisposePlaywright(disposalExceptions);
        DisposeConfigureLock(disposalExceptions);
        GC.SuppressFinalize(this);

        if (disposalExceptions.Count > 0) throw new AggregateException("One or more errors occurred while disposing PlaywrightService.", disposalExceptions);
    }

    private async Task CloseContextAsync(List<Exception> disposalExceptions)
    {
        if (context is null) return;

        try
        {
            await context.CloseAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            disposalExceptions.Add(exception);
        }
    }

    private void DisposePlaywright(List<Exception> disposalExceptions)
    {
        try
        {
            playwright?.Dispose();
        }
        catch (Exception exception)
        {
            disposalExceptions.Add(exception);
        }
    }

    private void DisposeConfigureLock(List<Exception> disposalExceptions)
    {
        try
        {
            configureLock.Dispose();
        }
        catch (Exception exception)
        {
            disposalExceptions.Add(exception);
        }
    }
}
