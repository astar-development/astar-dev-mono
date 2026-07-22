using System.Reflection;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using NSubstitute.ExceptionExtensions;
using Testably.Abstractions.Testing;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Services;

public sealed class PlaywrightServiceFixture : IAsyncLifetime, IAsyncDisposable
{
    public string UserDataDirectory { get; } = Path.Combine(Path.GetTempPath(), $"playwright-profile-{Guid.NewGuid():N}");

    public MockFileSystem FileSystem { get; } = new();

    public IPlaywrightService Service { get; private set; } = null!;

    public IPage Page { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<PlaywrightService>();
        var scrapeConfiguration = Options.Create(new ScrapeConfiguration
        {
            UserDataDirectory = UserDataDirectory,
            SearchConfiguration = new SearchConfiguration { BaseUrl = new Uri("https://localhost"), UseHeadless = true },
        });
        Service = new PlaywrightService(logger, scrapeConfiguration, FileSystem);

        var result = await Service.ConfigurePlaywrightAsync(CancellationToken.None);
        Page = ((Success<IPage>)result).Value;
    }

    public async ValueTask DisposeAsync()
    {
        await ((IAsyncDisposable)Service).DisposeAsync();

        if (Directory.Exists(UserDataDirectory))
        {
            Directory.Delete(UserDataDirectory, true);
        }
    }
}

public sealed class GivenPlaywrightService(PlaywrightServiceFixture fixture) : IClassFixture<PlaywrightServiceFixture>, IDisposable
{
    private readonly string userDataDirectory = Path.Combine(Path.GetTempPath(), $"playwright-profile-{Guid.NewGuid():N}");
    private readonly MockFileSystem fileSystem = new();

    [Fact]
    public void when_configure_playwright_async_is_called_then_it_returns_an_ipage_instance() =>
        fixture.Page.ShouldBeAssignableTo<IPage>();

    [Fact]
    public void when_configure_playwright_async_is_called_then_the_browser_profile_is_persisted_to_the_configured_user_data_directory()
    {
        Directory.Exists(fixture.UserDataDirectory).ShouldBeTrue();
        Directory.EnumerateFileSystemEntries(fixture.UserDataDirectory).ShouldNotBeEmpty();
    }

    [Fact]
    public void when_configure_playwright_async_is_called_then_the_user_data_directory_is_created_via_the_injected_file_system() =>
        fixture.FileSystem.Directory.Exists(fixture.UserDataDirectory).ShouldBeTrue();

    [Fact]
    public async Task when_configure_playwright_async_is_called_then_the_browser_is_launched_using_the_chrome_channel()
    {
        var page = fixture.Page;
        await page.RouteAsync("https://localhost/**", route => route.FulfillAsync(new RouteFulfillOptions { ContentType = "text/html", Body = "<html></html>" }));

        await page.GotoAsync("https://localhost/browser-brand-check");
        string brands = await page.EvaluateAsync<string>("JSON.stringify((navigator.userAgentData?.brands ?? []).map(b => b.brand))");

        brands.ShouldContain("Google Chrome");
    }

    [Fact]
    public async Task when_configure_playwright_async_is_called_then_the_context_uses_the_configured_base_url()
    {
        var page = fixture.Page;
        await page.RouteAsync("https://localhost/**", route => route.FulfillAsync(new RouteFulfillOptions { ContentType = "text/html", Body = "<html></html>" }));

        await page.GotoAsync("/base-url-check");

        page.Url.ShouldBe("https://localhost/base-url-check");
    }

    [Fact]
    public async Task when_configure_playwright_async_is_called_then_the_browser_honours_the_configured_headless_setting()
    {
        var sut = CreatePlaywrightService(useHeadless: false);

        var result = await sut.ConfigurePlaywrightAsync(TestContext.Current.CancellationToken);

        var page = result.ShouldBeOfType<Success<IPage>>().Value;
        string userAgent = await page.EvaluateAsync<string>("navigator.userAgent");
        await page.Context.CloseAsync();
        userAgent.ShouldNotContain("HeadlessChrome");
    }

    [Fact]
    public async Task when_configure_playwright_async_is_called_then_navigator_webdriver_is_hidden_from_bot_detection_scripts()
    {
        var page = fixture.Page;
        await page.RouteAsync("https://localhost/**", route => route.FulfillAsync(new RouteFulfillOptions { ContentType = "text/html", Body = "<html></html>" }));

        await page.GotoAsync("https://localhost/webdriver-check");
        object? webdriver = await page.EvaluateAsync<object?>("navigator.webdriver");

        webdriver.ShouldBeNull();
    }

    [Fact]
    public async Task when_the_browser_launch_fails_then_a_failure_result_is_returned_instead_of_throwing()
    {
        var sut = CreatePlaywrightService(userDataDirectoryOverride: "\0invalid-user-data-directory");

        var result = await sut.ConfigurePlaywrightAsync(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Failure<IPage>>();
    }

    [Fact]
    public async Task when_the_base_url_has_a_path_segment_then_relative_navigation_resolves_beneath_that_path()
    {
        var sut = CreatePlaywrightService(baseUrlOverride: new Uri("https://localhost/gallery"));

        var result = await sut.ConfigurePlaywrightAsync(TestContext.Current.CancellationToken);

        var page = result.ShouldBeOfType<Success<IPage>>().Value;
        await page.RouteAsync("https://localhost/**", route => route.FulfillAsync(new RouteFulfillOptions { ContentType = "text/html", Body = "<html></html>" }));
        await page.GotoAsync("page2");
        string url = page.Url;
        await page.Context.CloseAsync();
        url.ShouldBe("https://localhost/gallery/page2");
    }

    [Fact]
    public async Task when_configure_playwright_async_is_called_twice_then_the_same_page_instance_is_returned()
    {
        var sut = CreatePlaywrightService();

        var firstResult = await sut.ConfigurePlaywrightAsync(TestContext.Current.CancellationToken);
        var secondResult = await sut.ConfigurePlaywrightAsync(TestContext.Current.CancellationToken);

        var firstPage = firstResult.ShouldBeOfType<Success<IPage>>().Value;
        var secondPage = secondResult.ShouldBeOfType<Success<IPage>>().Value;
        await firstPage.Context.CloseAsync();
        secondPage.ShouldBeSameAs(firstPage);
    }

    [Fact]
    public async Task when_configure_playwright_async_is_called_concurrently_then_a_single_page_is_shared()
    {
        var sut = CreatePlaywrightService();

        var results = await Task.WhenAll(sut.ConfigurePlaywrightAsync(TestContext.Current.CancellationToken), sut.ConfigurePlaywrightAsync(TestContext.Current.CancellationToken));

        var firstPage = results[0].ShouldBeOfType<Success<IPage>>().Value;
        var secondPage = results[1].ShouldBeOfType<Success<IPage>>().Value;
        await firstPage.Context.CloseAsync();
        secondPage.ShouldBeSameAs(firstPage);
    }

    [Fact]
    public async Task when_a_failed_launch_is_followed_by_a_corrected_configuration_then_a_page_is_returned()
    {
        var configuration = new ScrapeConfiguration { UserDataDirectory = "\0invalid-user-data-directory", SearchConfiguration = new SearchConfiguration { BaseUrl = new Uri("https://localhost"), UseHeadless = true } };
        var sut = CreatePlaywrightService(configuration);

        var firstResult = await sut.ConfigurePlaywrightAsync(TestContext.Current.CancellationToken);
        configuration.UserDataDirectory = userDataDirectory;
        var secondResult = await sut.ConfigurePlaywrightAsync(TestContext.Current.CancellationToken);

        firstResult.ShouldBeOfType<Failure<IPage>>();
        var page = secondResult.ShouldBeOfType<Success<IPage>>().Value;
        await page.Context.CloseAsync();
    }

    [Fact]
    public async Task when_the_service_is_disposed_then_the_page_is_closed()
    {
        var sut = CreatePlaywrightService();
        var result = await sut.ConfigurePlaywrightAsync(TestContext.Current.CancellationToken);
        var page = result.ShouldBeOfType<Success<IPage>>().Value;

        await ((IAsyncDisposable)sut).DisposeAsync();

        page.IsClosed.ShouldBeTrue();
    }

    [Fact]
    public void when_created_then_it_implements_iasyncdisposable()
    {
        var sut = CreatePlaywrightService();

        sut.ShouldBeAssignableTo<IAsyncDisposable>();
    }

    [Fact]
    public async Task when_context_close_async_throws_during_dispose_then_playwright_and_the_configure_lock_are_still_disposed()
    {
        var sut = (PlaywrightService)CreatePlaywrightService();
        var throwingContext = Substitute.For<IBrowserContext>();
        throwingContext.CloseAsync().ThrowsAsync(new InvalidOperationException("context close failed"));
        var substitutePlaywright = Substitute.For<IPlaywright>();
        SetPrivateField(sut, "context", throwingContext);
        SetPrivateField(sut, "playwright", substitutePlaywright);

        await Should.ThrowAsync<AggregateException>(async () => await ((IAsyncDisposable)sut).DisposeAsync());

        substitutePlaywright.Received(1).Dispose();
        var configureLock = GetPrivateField<SemaphoreSlim>(sut, "configureLock");
        Should.Throw<ObjectDisposedException>(() => configureLock.Wait(0));
    }

    public void Dispose()
    {
        if (Directory.Exists(userDataDirectory))
        {
            Directory.Delete(userDataDirectory, true);
        }
    }

    private IPlaywrightService CreatePlaywrightService(bool useHeadless = true, string? userDataDirectoryOverride = null, Uri? baseUrlOverride = null) =>
        CreatePlaywrightService(new ScrapeConfiguration { UserDataDirectory = userDataDirectoryOverride ?? userDataDirectory, SearchConfiguration = new SearchConfiguration { BaseUrl = baseUrlOverride ?? new Uri("https://localhost"), UseHeadless = useHeadless } });

    private IPlaywrightService CreatePlaywrightService(ScrapeConfiguration configuration)
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<PlaywrightService>();

        return new PlaywrightService(logger, Options.Create(configuration), fileSystem);
    }

    private static void SetPrivateField(PlaywrightService target, string fieldName, object value) =>
        typeof(PlaywrightService).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(target, value);

    private static TField GetPrivateField<TField>(PlaywrightService target, string fieldName) =>
        (TField)typeof(PlaywrightService).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(target)!;
}
