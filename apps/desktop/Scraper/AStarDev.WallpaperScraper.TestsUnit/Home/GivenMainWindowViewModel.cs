using System.Reactive.Concurrency;
using AStarDev.WallpaperScraper.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReactiveUI;
using Microsoft.Playwright;
using NSubstitute.Core;
using AStar.Dev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Home;
using AStarDev.WallpaperScraper.Scrapers;

namespace AStarDev.WallpaperScraper.TestsUnit.Home;

public sealed class GivenMainWindowViewModel
{
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
    public void when_a_scrape_command_is_executing_then_is_busy_is_true()
    {
        var completionSource = new TaskCompletionSource<Exceptional<UnitFp>>();
        var scrapeOrchestrator = Substitute.For<IScrapeOrchestrator>();
        scrapeOrchestrator.ScrapeSearchCategoriesAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>()).Returns(completionSource.Task);
        var sut = CreateViewModel(scrapeOrchestrator: scrapeOrchestrator);

        sut.ScrapeSearchCategoriesCommand.Execute().Subscribe();

        sut.IsBusy.ShouldBeTrue();

        completionSource.SetResult(new Success<UnitFp>(UnitFp.Instance));
    }

    [Fact]
    public void when_a_scrape_command_completes_then_is_busy_is_false_again()
    {
        var completionSource = new TaskCompletionSource<Exceptional<UnitFp>>();
        var scrapeOrchestrator = Substitute.For<IScrapeOrchestrator>();
        scrapeOrchestrator.ScrapeSearchCategoriesAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>()).Returns(completionSource.Task);
        var sut = CreateViewModel(scrapeOrchestrator: scrapeOrchestrator);
        sut.ScrapeSearchCategoriesCommand.Execute().Subscribe();

        completionSource.SetResult(new Success<UnitFp>(UnitFp.Instance));

        sut.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public void when_a_scrape_command_is_executing_then_a_second_different_scrape_command_cannot_execute()
    {
        var completionSource = new TaskCompletionSource<Exceptional<UnitFp>>();
        var scrapeOrchestrator = Substitute.For<IScrapeOrchestrator>();
        scrapeOrchestrator.ScrapeSearchCategoriesAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>()).Returns(completionSource.Task);
        var sut = CreateViewModel(scrapeOrchestrator: scrapeOrchestrator);

        sut.ScrapeSearchCategoriesCommand.Execute().Subscribe();

        bool canExecuteTop = false;
        using var subscription = sut.ScrapeTopCommand.CanExecute.Subscribe(canExecute => canExecuteTop = canExecute);

        canExecuteTop.ShouldBeFalse();

        completionSource.SetResult(new Success<UnitFp>(UnitFp.Instance));
    }

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

    [Fact]
    public async Task when_scrape_search_categories_command_is_executed_then_the_scrape_orchestrator_is_called()
    {
        var scrapeOrchestrator = Substitute.For<IScrapeOrchestrator>();
        scrapeOrchestrator.ScrapeSearchCategoriesAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Exceptional<UnitFp>>(new Success<UnitFp>(UnitFp.Instance)));

        var sut = CreateViewModel(scrapeOrchestrator: scrapeOrchestrator);

        sut.ScrapeSearchCategoriesCommand.Execute().Subscribe();

        await scrapeOrchestrator.Received().ScrapeSearchCategoriesAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_scrape_top_command_is_executed_then_the_scrape_orchestrator_is_called()
    {
        var scrapeOrchestrator = Substitute.For<IScrapeOrchestrator>();
        scrapeOrchestrator.ScrapeTopAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Exceptional<UnitFp>>(new Success<UnitFp>(UnitFp.Instance)));
        var sut = CreateViewModel(scrapeOrchestrator: scrapeOrchestrator);

        sut.ScrapeTopCommand.Execute().Subscribe();

        await scrapeOrchestrator.Received().ScrapeTopAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_scrape_subscribed_command_is_executed_then_the_scrape_orchestrator_is_called()
    {
        var scrapeOrchestrator = Substitute.For<IScrapeOrchestrator>();
        scrapeOrchestrator.ScrapeSubscribedAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Exceptional<UnitFp>>(new Success<UnitFp>(UnitFp.Instance)));
        var sut = CreateViewModel(scrapeOrchestrator: scrapeOrchestrator);

        sut.ScrapeSubscribedCommand.Execute().Subscribe();

        await scrapeOrchestrator.Received().ScrapeSubscribedAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_scrape_all_command_is_executed_then_the_scrape_orchestrator_is_called()
    {
        var scrapeOrchestrator = Substitute.For<IScrapeOrchestrator>();
        scrapeOrchestrator.ScrapeAllAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Exceptional<UnitFp>>(new Success<UnitFp>(UnitFp.Instance)));
        var sut = CreateViewModel(scrapeOrchestrator: scrapeOrchestrator);
        sut.ScrapeAllCommand.Execute().Subscribe();

        await scrapeOrchestrator.Received().ScrapeAllAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_constructed_then_status_messages_is_empty() =>
        CreateViewModel().StatusMessages.ShouldBeEmpty();

    [Fact]
    public void when_a_scrape_method_reports_progress_then_the_message_is_added_to_status_messages()
    {
        var scrapeOrchestrator = Substitute.For<IScrapeOrchestrator>();
        scrapeOrchestrator.ScrapeSearchCategoriesAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<IProgress<string>>().Report("Message 1");

                return Task.FromResult<Exceptional<UnitFp>>(new Success<UnitFp>(UnitFp.Instance));
            });
        var sut = CreateViewModel(scrapeOrchestrator: scrapeOrchestrator);

        sut.ScrapeSearchCategoriesCommand.Execute().Subscribe();

        sut.StatusMessages.ShouldContain("Message 1");
    }

    [Fact]
    public void when_multiple_messages_are_reported_then_the_newest_message_is_first()
    {
        var scrapeOrchestrator = Substitute.For<IScrapeOrchestrator>();
        scrapeOrchestrator.ScrapeSearchCategoriesAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var progress = callInfo.Arg<IProgress<string>>();
                progress.Report("Message 1");
                progress.Report("Message 2");

                return Task.FromResult<Exceptional<UnitFp>>(new Success<UnitFp>(UnitFp.Instance));
            });
        var sut = CreateViewModel(scrapeOrchestrator: scrapeOrchestrator);

        sut.ScrapeSearchCategoriesCommand.Execute().Subscribe();

        sut.StatusMessages.First().ShouldBe("Message 2");
    }

    [Fact]
    public void when_more_than_500_messages_are_reported_then_only_the_newest_500_are_kept()
    {
        var scrapeOrchestrator = Substitute.For<IScrapeOrchestrator>();
        scrapeOrchestrator.ScrapeSearchCategoriesAsync(Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var progress = callInfo.Arg<IProgress<string>>();
                for (int index = 1; index <= 501; index++)
                    progress.Report($"Message {index}");

                return Task.FromResult<Exceptional<UnitFp>>(new Success<UnitFp>(UnitFp.Instance));
            });
        var sut = CreateViewModel(scrapeOrchestrator: scrapeOrchestrator);

        sut.ScrapeSearchCategoriesCommand.Execute().Subscribe();

        sut.StatusMessages.Count.ShouldBe(500);
        sut.StatusMessages.First().ShouldBe("Message 501");
        sut.StatusMessages.Last().ShouldBe("Message 2");
    }

    private static MainWindowViewModel CreateViewModel(
        IScrapeOrchestrator scrapeOrchestrator = null!,
        Exceptional<IPage>? configureResult = null,
        Func<CallInfo, Task<Exceptional<IPage>>>? configureBehavior = null,
        Exceptional<UnitFp>? scrapeActionResult = null,
        Func<CallInfo, Task<Exceptional<UnitFp>>>? scrapeActionBehavior = null,
        bool? confirmScrape = true,
        string applicationName = "Test App")
    {
        scrapeOrchestrator ??= Substitute.For<IScrapeOrchestrator>();
        var scrapeConfiguration = Options.Create(new ScrapeConfiguration { ApplicationName = applicationName, WindowSize = new WindowSize(1_234, 567) });
        var sut = new MainWindowViewModel(scrapeConfiguration, scrapeOrchestrator, new NullLogger<MainWindowViewModel>());

        return sut;
    }

    private sealed class ImmediateSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);
    }
}
