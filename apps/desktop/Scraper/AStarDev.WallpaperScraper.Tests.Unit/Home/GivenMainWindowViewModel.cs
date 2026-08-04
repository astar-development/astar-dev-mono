using System.Reactive.Concurrency;
using AStarDev.WallpaperScraper.Configuration;
using AStarDev.WallpaperScraper.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReactiveUI;
using Microsoft.Playwright;
using NSubstitute.Core;
using RxUnit = System.Reactive.Unit;
using AStar.Dev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Home;

namespace AStarDev.WallpaperScraper.Tests.Unit.Home;

public sealed class GivenMainWindowViewModel
{
    private readonly IPlaywrightService playwrightService = Substitute.For<IPlaywrightService>();

    static GivenMainWindowViewModel() => RxApp.MainThreadScheduler = ImmediateScheduler.Instance;

    public GivenMainWindowViewModel() => SynchronizationContext.SetSynchronizationContext(new ImmediateSynchronizationContext());

    [Fact]
    public async Task when_main_window_loads_then_the_width_is_set_from_the_appsettings()
    {
        var sut = CreateViewModel();

        sut.WindowWidth.ShouldBe(1_234);
    }

    [Fact]
    public async Task when_main_window_loads_then_the_height_is_set_from_the_appsettings()
    {
        var sut = CreateViewModel();

        sut.WindowHeight.ShouldBe(567);
    }

    [Fact]
    public void when_main_window_loads_then_the_title_is_composed_from_the_application_name_and_version()
    {
        var sut = CreateViewModel();

        sut.Title.ShouldBe($"Test App V{MainWindowViewModel.ApplicationVersion}");
    }

    [Fact]
    public void when_application_name_is_empty_then_the_title_still_contains_the_version()
    {
        var sut = CreateViewModel(applicationName: string.Empty);

        sut.Title.ShouldBe($" V{MainWindowViewModel.ApplicationVersion}");
    }

    [Fact]
    public void when_application_version_is_accessed_then_it_is_not_null_or_whitespace() =>
        MainWindowViewModel.ApplicationVersion.ShouldNotBeNullOrWhiteSpace();

    [Fact]
    public void when_application_version_is_accessed_then_it_does_not_contain_a_source_link_suffix() =>
        MainWindowViewModel.ApplicationVersion.ShouldNotContain('+');

    private MainWindowViewModel CreateViewModel(
        Exceptional<IPage>? configureResult = null,
        Func<CallInfo, Task<Exceptional<IPage>>>? configureBehavior = null,
        Exceptional<UnitFp>? scrapeActionResult = null,
        Func<CallInfo, Task<Exceptional<UnitFp>>>? scrapeActionBehavior = null,
        bool? confirmScrape = true,
        string applicationName = "Test App")
    {
        playwrightService.ConfigurePlaywrightAsync(Arg.Any<CancellationToken>())
            .Returns(configureBehavior ?? (_ => Task.FromResult(configureResult ?? Exceptional.Success(Substitute.For<IPage>()))));

        var scrapeConfiguration = Options.Create(new ScrapeConfiguration { ApplicationName = applicationName, WindowSize = new WindowSize(1_234, 567) });
        var sut = new MainWindowViewModel(scrapeConfiguration, playwrightService, new NullLogger<MainWindow>());

        return sut;
    }

    private sealed class ImmediateSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);
    }
}
