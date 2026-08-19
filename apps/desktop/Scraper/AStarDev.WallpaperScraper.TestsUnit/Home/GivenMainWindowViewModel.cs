using System.Reactive.Concurrency;
using AStarDev.WallpaperScraper.Configuration;
using AStarDev.WallpaperScraper.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReactiveUI;
using Microsoft.Playwright;
using NSubstitute.Core;
using AStar.Dev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Home;

namespace AStarDev.WallpaperScraper.TestsUnit.Home;

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

    [Fact]
    public void should_contain_the_OpenConnectionStringsCommand() =>
        CreateViewModel().OpenConnectionStringsCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_OpenFileClassificationCategoriesCommand() =>
        CreateViewModel().OpenFileClassificationCategoriesCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_OpenSearchConfigurationCommand() =>
        CreateViewModel().OpenSearchConfigurationCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_OpenModelToIgnoreCommand() =>
        CreateViewModel().OpenModelToIgnoreCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_OpenScrapeDirectoriesCommand() =>
        CreateViewModel().OpenScrapeDirectoriesCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_OpenSearchCategoriesCommand() =>
        CreateViewModel().OpenSearchCategoriesCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_OpenTagToIgnoreCommand() =>
        CreateViewModel().OpenTagToIgnoreCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_OpenUserConfigurationCommand() =>
        CreateViewModel().OpenUserConfigurationCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_ResetDatabaseAndDirectoriesCommand() =>
        CreateViewModel().ResetDatabaseAndDirectoriesCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_IsBusy_property() =>
        CreateViewModel().IsBusy.ShouldBeFalse();

    [Fact]
    public void should_contain_the_ScrapeSearchCategoriesCommand() =>
        CreateViewModel().ScrapeSearchCategoriesCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_ScrapeTopCommand() =>
        CreateViewModel().ScrapeTopCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_ScrapeSubscribedCommand() =>
        CreateViewModel().ScrapeSubscribedCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_ScrapeAllCommand() =>
        CreateViewModel().ScrapeAllCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

    [Fact]
    public void should_contain_the_CancelCommand() =>
        CreateViewModel().CancelCommand.ShouldBeOfType<ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>>();

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
        var sut = new MainWindowViewModel(scrapeConfiguration, playwrightService, new NullLogger<MainWindowViewModel>());

        return sut;
    }

    private sealed class ImmediateSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);
    }
}
