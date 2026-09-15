using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using AStarDev.WallpaperScraper.Configuration;
using AStarDev.WallpaperScraper.Localization;
using AStarDev.WallpaperScraper.Scrapers;
using AStarDev.WallpaperScraper.Services;
using AStarDev.WallpaperScraper.Startup;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using ReactiveUI;

namespace AStarDev.WallpaperScraper.Home;

public class MainWindowViewModel : ReactiveObject, IDisposable
{
    private const int MaxStatusMessages = 500;

    private readonly IScrapeOrchestrator scrapeOrchestrator;
    private readonly IPlaywrightService playwrightService;
    private readonly ILogger<MainWindowViewModel> logger;
    private readonly StartupDiagnostics startupDiagnostics;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly Progress<string> statusProgress;
    private bool disposed;

    public MainWindowViewModel(IOptions<ScraperAppConfiguration> scrapeConfiguration, IScrapeOrchestrator scrapeOrchestrator, IPlaywrightService playwrightService, ILogger<MainWindowViewModel> logger, StartupDiagnostics startupDiagnostics, ILocalizationService localizationService)
    {
        cancellationTokenSource = new CancellationTokenSource();
        statusProgress = new Progress<string>(AddStatusMessage);
        string userDataDirectory = scrapeConfiguration.Value.UserDataDirectory;
        LogMessage.Information(logger, "MainWindowViewModel initialized with UserDataDirectory: {UserDataDirectory}", userDataDirectory);
        Title = $"{scrapeConfiguration.Value.ApplicationName} V{ApplicationVersion}";
        SetWindowSize(scrapeConfiguration.Value.WindowSize);
        this.scrapeOrchestrator = scrapeOrchestrator;
        this.playwrightService = playwrightService;
        this.logger = logger;
        this.startupDiagnostics = startupDiagnostics;
        StartupErrorMessage = ComposeStartupErrorMessage(startupDiagnostics, localizationService);
        ScrapeSearchCategoriesCommand = CreateScrapeCommand("Scrape Search Categories", null!);
        ScrapeTopCommand = CreateScrapeCommand("Scrape Top Wallpapers", null!);
        ScrapeSubscribedCommand = CreateScrapeCommand("Scrape Subscribed Wallpapers", null!);
        ScrapeAllCommand = CreateScrapeCommand("Scrape All Wallpapers", null!);
        CancelCommand = ReactiveCommand.Create(CancelRunningScrape, this.WhenAnyValue(vm => vm.IsBusy));

        OpenConnectionStringsCommand = CreateOpenEditorCommand(() => "entityEditorFactory.CreateConnectionStringsEditor");
        OpenFileClassificationCategoriesCommand = CreateOpenEditorCommand(() => "entityEditorFactory.CreateFileClassificationCategoriesEditor");
        OpenSearchConfigurationCommand = CreateOpenEditorCommand(() => "entityEditorFactory.CreateSearchConfigurationEditor");
        OpenModelToIgnoreCommand = CreateOpenEditorCommand(() => "entityEditorFactory.CreateModelToIgnoreEditor");
        OpenScrapeDirectoriesCommand = CreateOpenEditorCommand(() => "entityEditorFactory.CreateScrapeDirectoriesEditor");
        OpenSearchCategoriesCommand = CreateOpenEditorCommand(() => "entityEditorFactory.CreateSearchCategoriesEditor");
        OpenTagToIgnoreCommand = CreateOpenEditorCommand(() => "entityEditorFactory.CreateTagToIgnoreEditor");
        OpenUserConfigurationCommand = CreateOpenEditorCommand(() => "entityEditorFactory.CreateUserConfigurationEditor");

        ResetDatabaseAndDirectoriesCommand = CreateResetDatabaseAndDirectoriesCommand();
    }

    public string Title { get; }
    public double WindowWidth { get; set; } = 1_000;
    public double WindowHeight { get; set; } = 1_000;

    /// <summary>
    ///     Real-time status messages reported by the running scrape, newest first, capped at
    ///     <see cref="MaxStatusMessages" /> entries.
    /// </summary>
    public ObservableCollection<string> StatusMessages { get; } = [];

    /// <summary>Opens the Connection Strings Configuration editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenConnectionStringsCommand { get; }

    /// <summary>Opens the File Classification Categories Configuration editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenFileClassificationCategoriesCommand { get; }

    /// <summary>Opens the Search Configuration editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenSearchConfigurationCommand { get; }

    /// <summary>Opens the Model to Ignore editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenModelToIgnoreCommand { get; }

    /// <summary>Opens the Scrape Directories editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenScrapeDirectoriesCommand { get; }

    /// <summary>Opens the Search Categories editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenSearchCategoriesCommand { get; }

    /// <summary>Opens the Tag to Ignore editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenTagToIgnoreCommand { get; }

    /// <summary>Opens the User Configuration editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenUserConfigurationCommand { get; }

    /// <summary>Clears the scraped data tables and, separately, deletes the downloaded files on disk, each behind its own confirmation prompt.</summary>
    public ReactiveCommand<Unit, Unit> ResetDatabaseAndDirectoriesCommand { get; }

    /// <summary>
    ///     Gets a value indicating whether a scrape command is currently running. Drives whether
    ///     <see cref="CancelCommand" /> can execute.
    /// </summary>
    public bool IsBusy
    {
        get => field;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Gets a value indicating whether one or more startup steps failed, e.g. a pending database
    ///     migration. While <see langword="true" />, the scrape commands cannot execute.
    /// </summary>
    public bool HasStartupError => startupDiagnostics.Failures.Count > 0;

    /// <summary>
    ///     Gets the message describing the startup failures, for display in a <see cref="MainWindow" />
    ///     banner. Empty when <see cref="HasStartupError" /> is <see langword="false" />.
    /// </summary>
    public string StartupErrorMessage { get; }

    public ReactiveCommand<Unit, Unit> ScrapeSearchCategoriesCommand { get; }

    public ReactiveCommand<Unit, Unit> ScrapeTopCommand { get; }

    public ReactiveCommand<Unit, Unit> ScrapeSubscribedCommand { get; }

    public ReactiveCommand<Unit, Unit> ScrapeAllCommand { get; }

    /// <summary>
    ///     Gets the command that cancels whichever scrape command is currently running.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>
    ///     The version CI stamps from the release tag (-p:Version=...), so the title can
    ///     never drift from the Velopack package version. SourceLink appends +sha; strip it.
    /// </summary>
    public static string ApplicationVersion { get; } = typeof(MainWindowViewModel).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion.Split('+')[0] ?? "0.0.0";

    public ReactiveCommand<Unit, Unit> ExitCommand { get; } = ReactiveCommand.Create(static () =>
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    });

    private void SetWindowSize(WindowSize windowSize)
    {
        WindowWidth = windowSize.Width;
        WindowHeight = windowSize.Height;
    }

    private static string ComposeStartupErrorMessage(StartupDiagnostics startupDiagnostics, ILocalizationService localizationService)
    {
        if (startupDiagnostics.Failures.Count == 0) return string.Empty;

        string reasons = string.Join("; ", startupDiagnostics.Failures.Select(failure => $"{failure.Step}: {failure.Exception.Message}"));

        return localizationService.GetLocal("Startup.Error.Banner", reasons);
    }


    private ReactiveCommand<Unit, Unit> CreateScrapeCommand(string actionName, IScrapeAction action)
    {
        LogMessage.Information(logger, "Creating command for action: {ActionName}", actionName);
        var canExecute = this.WhenAnyValue(vm => vm.IsBusy).Select(busy => !busy && !HasStartupError);

        Func<IPage, Task<Exceptional<UnitFp>>> scrape = actionName switch
        {
            "Scrape Search Categories" => page => scrapeOrchestrator.ScrapeSearchCategoriesAsync(statusProgress, page, cancellationTokenSource!.Token),
            "Scrape Top Wallpapers" => page => scrapeOrchestrator.ScrapeTopAsync(statusProgress, page, cancellationTokenSource!.Token),
            "Scrape Subscribed Wallpapers" => page => scrapeOrchestrator.ScrapeSubscribedAsync(statusProgress, page, cancellationTokenSource!.Token),
            "Scrape All Wallpapers" => page => scrapeOrchestrator.ScrapeAllAsync(statusProgress, page, cancellationTokenSource!.Token),
            _ => throw new ArgumentException($"Unknown action name: {actionName}", nameof(actionName)),
        };

        var command = ReactiveCommand.CreateFromTask(() => RunScrapeAsync(scrape), canExecute);
        command.ThrownExceptions.Subscribe(exception => HandleScrapeCommandException(actionName, exception));

        return command;
    }

    private void HandleScrapeCommandException(string actionName, Exception exception)
    {
        LogMessage.Error(logger, actionName, exception);
        AddStatusMessage($"'{actionName}' failed: {exception.Message}");
    }

    private async Task RunScrapeAsync(Func<IPage, Task<Exceptional<UnitFp>>> scrape)
    {
        IsBusy = true;
        try
        {
            var page = await playwrightService.ConfigurePlaywrightAsync(cancellationTokenSource!.Token)
                .MatchAsync(p => p, exception => throw new InvalidOperationException("Failed to configure Playwright.", exception));

            await scrape(page);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ReactiveCommand<Unit, Unit> CreateOpenEditorCommand(Func<string> createEditor)
    {
        string message = createEditor();
        LogMessage.Information(logger, "Creating command for editor: {EditorName}", message);

        return ReactiveCommand.Create(static () => { });
    }

    private ReactiveCommand<Unit, Unit> CreateResetDatabaseAndDirectoriesCommand()
    {
        string message = "ResetDatabaseAndDirectoriesCommand";
        LogMessage.Information(logger, "Creating command for editor: {EditorName}", message);

        return ReactiveCommand.Create(static () => { });
    }

    private void CancelRunningScrape() => cancellationTokenSource?.Cancel();

    private void AddStatusMessage(string message)
    {
        StatusMessages.Insert(0, message);
        if (StatusMessages.Count > MaxStatusMessages)
            StatusMessages.RemoveAt(StatusMessages.Count - 1);
    }

    /// <summary>Releases the resources held by the application's dependency injection container.</summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;

        disposed = true;
        if (disposing)
        {
            cancellationTokenSource.Dispose();
        }
    }

    /// <summary>Releases the resources held by the application's dependency injection container.</summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method - Do NOT remove this comment.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
