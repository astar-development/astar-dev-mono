using Microsoft.Playwright;

namespace Fab4Kids.Web.TestsIntegration;

public sealed class SmokeTestFixture : IAsyncLifetime
{
    private IPlaywright? playwright;
    private IBrowser? browser;

    public string? BaseUrl { get; } = Environment.GetEnvironmentVariable("FAB4KIDS_SMOKE_URL");

    public IBrowser Browser => browser ?? throw new InvalidOperationException("Browser is not initialised; FAB4KIDS_SMOKE_URL was not set.");

    public async ValueTask InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            return;

        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (browser is not null)
            await browser.CloseAsync();

        playwright?.Dispose();
    }
}
