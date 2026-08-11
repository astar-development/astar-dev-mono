using System.Reactive;
using System.Reflection;
using AStar.Dev.Logging.Extensions;
using AStarDev.WallpaperScraper.Configuration;
using AStarDev.WallpaperScraper.Scrapers;
using AStarDev.WallpaperScraper.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactiveUI;

namespace AStarDev.WallpaperScraper.Home;

public class MainWindowViewModel : ReactiveObject, IDisposable
{
    private readonly IPlaywrightService playwrightService;
    private readonly ILogger<MainWindow> logger;
    private readonly CancellationTokenSource cancellationTokenSource;
    private bool disposed;

    public MainWindowViewModel(IOptions<ScrapeConfiguration> scrapeConfiguration, IPlaywrightService playwrightService, ILogger<MainWindow> logger)
    {
        cancellationTokenSource = new CancellationTokenSource();
        string userDataDirectory = scrapeConfiguration.Value.UserDataDirectory;
        LogMessage.Information(logger, "MainWindowViewModel initialized with UserDataDirectory: {UserDataDirectory}", userDataDirectory);
        Title = $"{scrapeConfiguration.Value.ApplicationName} V{ApplicationVersion}";
        SetWindowSize(scrapeConfiguration.Value.WindowSize);
        this.playwrightService = playwrightService;
        this.logger = logger;
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
    private ReactiveCommand<Unit, Unit> CreateScrapeCommand(string actionName, IScrapeAction action)
    {
        LogMessage.Information(logger, "Creating command for action: {ActionName}", actionName);

        return null!;
    }

    private ReactiveCommand<Unit, Unit> CreateOpenEditorCommand(Func<string> createEditor)
    {
        string message = createEditor();
        LogMessage.Information(logger, "Creating command for editor: {EditorName}", message);

        return null!;
    }

    private ReactiveCommand<Unit, Unit> CreateResetDatabaseAndDirectoriesCommand()
    {
        string message = "ResetDatabaseAndDirectoriesCommand";
        LogMessage.Information(logger, "Creating command for editor: {EditorName}", message);

        return null!;
    }

    private void CancelRunningScrape() => cancellationTokenSource?.Cancel();

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
