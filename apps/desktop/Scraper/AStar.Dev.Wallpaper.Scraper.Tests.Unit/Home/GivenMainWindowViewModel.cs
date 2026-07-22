using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using AStar.Dev.Wallpaper.Scraper.Home;
using AStar.Dev.Wallpaper.Scraper.Maintenance;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Theming;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using NSubstitute.Core;
using ReactiveUI;
using RxUnit = System.Reactive.Unit;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Home;

public sealed class GivenMainWindowViewModel
{
    private readonly IPlaywrightService playwrightService = Substitute.For<IPlaywrightService>();
    private readonly IScrapeAction searchCategoryScrapeAction = Substitute.For<IScrapeAction>();
    private readonly IEntityEditorFactory entityEditorFactory = Substitute.For<IEntityEditorFactory>();
    private readonly IThemeService themeService = Substitute.For<IThemeService>();
    private readonly IDatabaseResetService databaseResetService = Substitute.For<IDatabaseResetService>();

    static GivenMainWindowViewModel()
    {
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;
    }

    public GivenMainWindowViewModel() =>
        SynchronizationContext.SetSynchronizationContext(new ImmediateSynchronizationContext());

    [Fact]
    public async Task when_search_categories_scrape_completes_then_the_injected_action_executes_against_the_configured_page()
    {
        var page = Substitute.For<IPage>();
        var sut = CreateViewModel(configureResult: Exceptional.Success(page));

        await sut.ScrapeSearchCategoriesCommand.Execute();

        await searchCategoryScrapeAction.Received().ExecuteAsync(page, Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_search_categories_action_reports_progress_then_the_status_text_includes_the_message()
    {
        var sut = CreateViewModel(scrapeActionBehavior: callInfo =>
        {
            callInfo.ArgAt<IProgress<string>>(1).Report("Visiting category: Nature");

            return Task.FromResult(Exceptional.Success(FunctionalParadigm.Unit.Instance));
        });

        await sut.ScrapeSearchCategoriesCommand.Execute();

        sut.StatusText.ShouldContain("Visiting category: Nature");
    }

    [Fact]
    public async Task when_the_search_categories_action_fails_then_the_status_reports_the_failure_message()
    {
        var sut = CreateViewModel(scrapeActionResult: Exceptional.Failure<FunctionalParadigm.Unit>(new InvalidOperationException("boom")));

        await sut.ScrapeSearchCategoriesCommand.Execute();

        sut.StatusText.ShouldContain("boom");
    }

    [Fact]
    public async Task when_the_search_categories_action_fails_with_a_non_cancellation_error_after_cancellation_has_been_requested_then_the_status_does_not_report_the_failure_message()
    {
        var actionEntered = new TaskCompletionSource();
        var actionGate = new TaskCompletionSource();
        var sut = CreateViewModel(scrapeActionBehavior: async _ =>
        {
            actionEntered.TrySetResult();
            await actionGate.Task;

            return Exceptional.Failure<FunctionalParadigm.Unit>(new InvalidOperationException("Protocol error: kaboom"));
        });

        var execution = sut.ScrapeSearchCategoriesCommand.Execute().ToTask();
        await actionEntered.Task;
        await sut.CancelCommand.Execute();
        actionGate.SetResult();
        await execution;

        sut.StatusText.ShouldNotContain("Protocol error");
        sut.StatusText.ShouldNotContain("kaboom");
        sut.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_status_text_exceeds_one_thousand_lines_then_the_oldest_lines_are_dropped()
    {
        var sut = CreateViewModel(scrapeActionBehavior: callInfo =>
        {
            var progress = callInfo.ArgAt<IProgress<string>>(1);

            for (int i = 0; i < 1005; i++)
            {
                progress.Report($"Message {i}");
            }

            return Task.FromResult(Exceptional.Success(FunctionalParadigm.Unit.Instance));
        });

        await sut.ScrapeSearchCategoriesCommand.Execute();

        sut.StatusText.ShouldNotContain("Message 0" + Environment.NewLine);
        sut.StatusText.ShouldContain("Message 1004");
        sut.StatusText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length.ShouldBeLessThanOrEqualTo(1000);
    }

    [Fact]
    public void when_constructed_then_cancel_command_cannot_execute()
    {
        var sut = CreateViewModel();

        bool canExecute = sut.CancelCommand.CanExecute.FirstAsync().Wait();

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_scrape_is_awaiting_confirmation_then_cancel_command_can_execute()
    {
        var sut = CreateViewModel(confirmScrape: null);
        var (confirmationGate, handlerEntered) = RegisterGatedConfirmHandler(sut);

        var execution = sut.ScrapeTopCommand.Execute().ToTask();
        await handlerEntered;

        sut.CancelCommand.CanExecute.FirstAsync().Wait().ShouldBeTrue();

        confirmationGate.SetResult(true);
        await execution;
    }

    [Fact]
    public async Task when_cancel_command_is_invoked_while_awaiting_confirmation_then_the_scrape_is_cancelled_before_playwright_is_configured()
    {
        var sut = CreateViewModel(confirmScrape: null);
        var (confirmationGate, handlerEntered) = RegisterGatedConfirmHandler(sut);

        var execution = sut.ScrapeTopCommand.Execute().ToTask();
        await handlerEntered;

        await sut.CancelCommand.Execute();
        confirmationGate.SetResult(true);
        await execution;

        sut.StatusText.ShouldContain("Cancelled");
        await playwrightService.DidNotReceive().ConfigurePlaywrightAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_cancel_command_is_invoked_then_the_scrape_command_is_no_longer_executing()
    {
        var sut = CreateViewModel(confirmScrape: null);
        var (confirmationGate, handlerEntered) = RegisterGatedConfirmHandler(sut);

        var execution = sut.ScrapeTopCommand.Execute().ToTask();
        await handlerEntered;

        await sut.CancelCommand.Execute();
        confirmationGate.SetResult(true);
        await execution;

        sut.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_scrape_completes_without_cancellation_then_playwright_is_configured_and_the_status_reports_success()
    {
        var sut = CreateViewModel();

        await sut.ScrapeTopCommand.Execute();

        sut.StatusText.ShouldContain("Playwright page configured successfully.");
        sut.StatusText.ShouldNotContain("Cancelled");
    }

    [Fact]
    public async Task when_disposed_while_a_scrape_is_awaiting_confirmation_then_the_scrape_is_cancelled()
    {
        var sut = CreateViewModel(confirmScrape: null);
        var (confirmationGate, handlerEntered) = RegisterGatedConfirmHandler(sut);

        var execution = sut.ScrapeTopCommand.Execute().ToTask();
        await handlerEntered;

        sut.Dispose();
        confirmationGate.SetResult(true);
        await execution;

        sut.StatusText.ShouldContain("Cancelled");
    }

    [Fact]
    public async Task when_the_confirmation_is_accepted_then_the_status_records_yes()
    {
        var sut = CreateViewModel();

        await sut.ScrapeTopCommand.Execute();

        sut.StatusText.ShouldContain("Scrape Top Wallpapers: Yes");
    }

    [Fact]
    public async Task when_the_confirmation_is_declined_then_the_status_records_no_and_the_scrape_does_not_run()
    {
        var sut = CreateViewModel(confirmScrape: false);

        await sut.ScrapeTopCommand.Execute();

        sut.StatusText.ShouldContain("Scrape Top Wallpapers: No");
        await playwrightService.DidNotReceive().ConfigurePlaywrightAsync(Arg.Any<CancellationToken>());
        sut.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public async Task when_playwright_configuration_fails_then_the_status_reports_the_configuration_error_and_the_action_does_not_execute()
    {
        var sut = CreateViewModel(configureResult: Exceptional.Failure<IPage>(new InvalidOperationException("kaboom")));

        await sut.ScrapeSearchCategoriesCommand.Execute();

        sut.StatusText.ShouldContain("Error configuring Playwright page: kaboom");
        await searchCategoryScrapeAction.DidNotReceive().ExecuteAsync(Arg.Any<IPage>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
        sut.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_scrape_command_has_no_action_then_the_configured_page_navigates_to_the_login_page()
    {
        var page = Substitute.For<IPage>();
        var sut = CreateViewModel(configureResult: Exceptional.Success(page));

        await sut.ScrapeTopCommand.Execute();

        _ = page.Received().GotoAsync("login");
    }

    [Fact]
    public async Task when_a_scrape_command_has_an_action_then_the_configured_page_navigates_to_the_root_page()
    {
        var page = Substitute.For<IPage>();
        var sut = CreateViewModel(configureResult: Exceptional.Success(page));

        await sut.ScrapeSearchCategoriesCommand.Execute();

        _ = page.Received().GotoAsync("/");
    }

    [Fact]
    public async Task when_cancel_command_is_invoked_while_playwright_is_configuring_then_the_action_does_not_execute()
    {
        var configureGate = new TaskCompletionSource<Exceptional<IPage>>();
        var configureEntered = new TaskCompletionSource();
        var sut = CreateViewModel(configureBehavior: _ =>
        {
            configureEntered.TrySetResult();

            return configureGate.Task;
        });

        var execution = sut.ScrapeSearchCategoriesCommand.Execute().ToTask();
        await configureEntered.Task;
        await sut.CancelCommand.Execute();
        configureGate.SetResult(Exceptional.Success(Substitute.For<IPage>()));
        await execution;

        sut.StatusText.ShouldContain("Cancelled");
        await searchCategoryScrapeAction.DidNotReceive().ExecuteAsync(Arg.Any<IPage>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
        sut.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_scrape_body_throws_unexpectedly_then_the_status_reports_the_error_and_the_command_is_no_longer_busy()
    {
        var sut = CreateViewModel(configureBehavior: _ => throw new InvalidOperationException("kaboom"));

        await Should.ThrowAsync<InvalidOperationException>(sut.ScrapeTopCommand.Execute().ToTask());

        sut.StatusText.ShouldContain("Scrape Top Wallpapers: Unexpected error - kaboom");
        sut.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public async Task when_open_connection_strings_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateConnectionStringsEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenConnectionStringsCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_file_classification_categories_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateFileClassificationCategoriesEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenFileClassificationCategoriesCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_search_configuration_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateSearchConfigurationEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenSearchConfigurationCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_model_to_ignore_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateModelToIgnoreEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenModelToIgnoreCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_scrape_directories_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateScrapeDirectoriesEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenScrapeDirectoriesCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_search_categories_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateSearchCategoriesEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenSearchCategoriesCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_tag_to_ignore_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateTagToIgnoreEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenTagToIgnoreCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_user_configuration_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateUserConfigurationEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenUserConfigurationCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public void when_constructed_then_the_selected_theme_reflects_the_theme_service_current_theme()
    {
        themeService.CurrentTheme.Returns(ThemeMode.Dark);

        var sut = CreateViewModel();

        sut.IsDarkThemeSelected.ShouldBeTrue();
        sut.IsSystemThemeSelected.ShouldBeFalse();
        sut.IsLightThemeSelected.ShouldBeFalse();
        sut.IsHackerThemeSelected.ShouldBeFalse();
    }

    [Fact]
    public async Task when_select_theme_command_executes_then_the_theme_service_applies_the_selected_theme()
    {
        var sut = CreateViewModel();

        await sut.SelectThemeCommand.Execute(ThemeMode.Hacker);

        themeService.Received().ApplyTheme(ThemeMode.Hacker);
    }

    [Fact]
    public async Task when_select_theme_command_executes_then_the_selected_theme_properties_update()
    {
        var sut = CreateViewModel();

        await sut.SelectThemeCommand.Execute(ThemeMode.Light);

        sut.IsLightThemeSelected.ShouldBeTrue();
        sut.IsSystemThemeSelected.ShouldBeFalse();
        sut.IsDarkThemeSelected.ShouldBeFalse();
        sut.IsHackerThemeSelected.ShouldBeFalse();
    }

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
    public async Task when_reset_database_and_directories_command_executes_and_both_prompts_are_confirmed_then_both_operations_run()
    {
        var sut = CreateViewModel(confirmScrape: null);
        RegisterResetConfirmationHandler(sut, resetDatabaseConfirmed: true, removeFilesConfirmed: true);

        await sut.ResetDatabaseAndDirectoriesCommand.Execute();

        await databaseResetService.Received().ResetDatabaseAsync(Arg.Any<CancellationToken>());
        await databaseResetService.Received().RemoveDownloadedFilesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_reset_database_and_directories_command_executes_and_the_database_prompt_is_declined_then_the_database_is_not_reset()
    {
        var sut = CreateViewModel(confirmScrape: null);
        RegisterResetConfirmationHandler(sut, resetDatabaseConfirmed: false, removeFilesConfirmed: true);

        await sut.ResetDatabaseAndDirectoriesCommand.Execute();

        await databaseResetService.DidNotReceive().ResetDatabaseAsync(Arg.Any<CancellationToken>());
        await databaseResetService.Received().RemoveDownloadedFilesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_reset_database_and_directories_command_executes_and_the_remove_files_prompt_is_declined_then_no_files_are_removed()
    {
        var sut = CreateViewModel(confirmScrape: null);
        RegisterResetConfirmationHandler(sut, resetDatabaseConfirmed: true, removeFilesConfirmed: false);

        await sut.ResetDatabaseAndDirectoriesCommand.Execute();

        await databaseResetService.Received().ResetDatabaseAsync(Arg.Any<CancellationToken>());
        await databaseResetService.DidNotReceive().RemoveDownloadedFilesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_reset_database_and_directories_command_executes_and_both_prompts_are_declined_then_neither_operation_runs()
    {
        var sut = CreateViewModel(confirmScrape: null);
        RegisterResetConfirmationHandler(sut, resetDatabaseConfirmed: false, removeFilesConfirmed: false);

        await sut.ResetDatabaseAndDirectoriesCommand.Execute();

        await databaseResetService.DidNotReceive().ResetDatabaseAsync(Arg.Any<CancellationToken>());
        await databaseResetService.DidNotReceive().RemoveDownloadedFilesAsync(Arg.Any<CancellationToken>());
    }

    private static void RegisterResetConfirmationHandler(MainWindowViewModel sut, bool resetDatabaseConfirmed, bool removeFilesConfirmed) =>
        sut.ConfirmScrape.RegisterHandler(context =>
        {
            context.SetOutput(context.Input == "Reset the database?" ? resetDatabaseConfirmed : removeFilesConfirmed);

            return Task.CompletedTask;
        });

    private MainWindowViewModel CreateViewModel(
        Exceptional<IPage>? configureResult = null,
        Func<CallInfo, Task<Exceptional<IPage>>>? configureBehavior = null,
        Exceptional<FunctionalParadigm.Unit>? scrapeActionResult = null,
        Func<CallInfo, Task<Exceptional<FunctionalParadigm.Unit>>>? scrapeActionBehavior = null,
        bool? confirmScrape = true)
    {
        playwrightService.ConfigurePlaywrightAsync(Arg.Any<CancellationToken>())
            .Returns(configureBehavior ?? (_ => Task.FromResult(configureResult ?? Exceptional.Success(Substitute.For<IPage>()))));

        searchCategoryScrapeAction.ExecuteAsync(Arg.Any<IPage>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(scrapeActionBehavior ?? (_ => Task.FromResult(scrapeActionResult ?? Exceptional.Success(FunctionalParadigm.Unit.Instance))));

        var scrapeConfiguration = Options.Create(new ScrapeConfiguration { ApplicationName = "Test App", WindowSize = new WindowSize(1_234, 567) });
        var sut = new MainWindowViewModel(scrapeConfiguration, playwrightService, searchCategoryScrapeAction, entityEditorFactory, themeService, databaseResetService);

        if (confirmScrape.HasValue)
        {
            bool confirmed = confirmScrape.Value;
            sut.ConfirmScrape.RegisterHandler(context => { context.SetOutput(confirmed); return Task.CompletedTask; });
        }

        return sut;
    }

    private static async Task<EntityEditorViewModelBase?> ExecuteAndCaptureOpenedEditor(MainWindowViewModel sut, ReactiveCommand<RxUnit, RxUnit> command)
    {
        EntityEditorViewModelBase? handled = null;
        sut.OpenEditor.RegisterHandler(context =>
        {
            handled = context.Input;
            context.SetOutput(RxUnit.Default);

            return Task.CompletedTask;
        });

        await command.Execute();

        return handled;
    }

    private static (TaskCompletionSource<bool> ConfirmationGate, Task HandlerEntered) RegisterGatedConfirmHandler(MainWindowViewModel sut)
    {
        var confirmationGate = new TaskCompletionSource<bool>();
        var handlerEntered = new TaskCompletionSource();
        sut.ConfirmScrape.RegisterHandler(async context =>
        {
            handlerEntered.TrySetResult();
            bool confirmed = await confirmationGate.Task;
            context.SetOutput(confirmed);
        });

        return (confirmationGate, handlerEntered.Task);
    }

    private sealed class ImmediateSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => d(state);
    }
}
