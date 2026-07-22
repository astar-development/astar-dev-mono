using System.Reactive.Linq;
using System.Reflection;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using AStar.Dev.Wallpaper.Scraper.Maintenance;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Theming;
using AStar.Dev.Wallpaper.Scraper.ViewModels;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace AStar.Dev.Wallpaper.Scraper.Home;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const int MaxStatusLines = 1_000;

    private string statusText = string.Empty;
    private string thumbnailCategoryName = string.Empty;
    private string thumbnailTags = string.Empty;
    private bool isBusy;
    private CancellationTokenSource? scrapeCancellationSource;
    private readonly IPlaywrightService playwrightService;
    private readonly IThemeService themeService;
    private readonly IDatabaseResetService databaseResetService;
    private readonly Queue<string> statusLines = new();
    private ThemeMode selectedTheme;

    public MainWindowViewModel(IOptions<ScrapeConfiguration> scrapeConfiguration, IPlaywrightService playwrightService, IScrapeAction searchCategoryScrapeAction,
        IEntityEditorFactory entityEditorFactory, IThemeService themeService, IDatabaseResetService databaseResetService)
    {
        Title = $"{scrapeConfiguration.Value.ApplicationName} V{ApplicationVersion}";
        this.playwrightService = playwrightService;
        this.themeService = themeService;
        this.databaseResetService = databaseResetService;
        SetWindowSize(scrapeConfiguration.Value.WindowSize);
        SetSelectedTheme(themeService.CurrentTheme);
        SelectThemeCommand = ReactiveCommand.Create<ThemeMode>(SelectTheme);

        ScrapeSearchCategoriesCommand = CreateScrapeCommand("Scrape Search Categories", searchCategoryScrapeAction);
        ScrapeTopCommand = CreateScrapeCommand("Scrape Top Wallpapers");
        ScrapeSubscribedCommand = CreateScrapeCommand("Scrape Subscribed Wallpapers");
        ScrapeAllCommand = CreateScrapeCommand("Scrape All Wallpapers");
        CancelCommand = ReactiveCommand.Create(CancelRunningScrape, this.WhenAnyValue(vm => vm.IsBusy));

        OpenConnectionStringsCommand = CreateOpenEditorCommand(entityEditorFactory.CreateConnectionStringsEditor);
        OpenFileClassificationCategoriesCommand = CreateOpenEditorCommand(entityEditorFactory.CreateFileClassificationCategoriesEditor);
        OpenSearchConfigurationCommand = CreateOpenEditorCommand(entityEditorFactory.CreateSearchConfigurationEditor);
        OpenModelToIgnoreCommand = CreateOpenEditorCommand(entityEditorFactory.CreateModelToIgnoreEditor);
        OpenScrapeDirectoriesCommand = CreateOpenEditorCommand(entityEditorFactory.CreateScrapeDirectoriesEditor);
        OpenSearchCategoriesCommand = CreateOpenEditorCommand(entityEditorFactory.CreateSearchCategoriesEditor);
        OpenTagToIgnoreCommand = CreateOpenEditorCommand(entityEditorFactory.CreateTagToIgnoreEditor);
        OpenUserConfigurationCommand = CreateOpenEditorCommand(entityEditorFactory.CreateUserConfigurationEditor);

        ResetDatabaseAndDirectoriesCommand = CreateResetDatabaseAndDirectoriesCommand();
    }

    private void SetWindowSize(WindowSize windowSize)
    {
        WindowWidth = windowSize.Width;
        WindowHeight = windowSize.Height;
    }

    public double WindowWidth { get; set; } = 1_000;
    public double WindowHeight { get; set; } = 1_000;

    public string Title { get; }

    public Interaction<string, bool> ConfirmScrape { get; } = new();

    public ReactiveCommand<Unit, Unit> ScrapeSearchCategoriesCommand { get; }

    public ReactiveCommand<Unit, Unit> ScrapeTopCommand { get; }

    public ReactiveCommand<Unit, Unit> ScrapeSubscribedCommand { get; }

    public ReactiveCommand<Unit, Unit> ScrapeAllCommand { get; }

    /// <summary>
    ///     Gets the command that cancels whichever scrape command is currently running.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>Raised by an Open* command to ask the view to display an <see cref="EntityEditorViewModelBase" /> as a modal dialog.</summary>
    public Interaction<EntityEditorViewModelBase, Unit> OpenEditor { get; } = new();

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

    /// <summary>Applies and persists the chosen <see cref="ThemeMode" />.</summary>
    public ReactiveCommand<ThemeMode, Unit> SelectThemeCommand { get; }

    /// <summary>Gets a value indicating whether <see cref="ThemeMode.System" /> is the currently selected theme.</summary>
    public bool IsSystemThemeSelected => selectedTheme == ThemeMode.System;

    /// <summary>Gets a value indicating whether <see cref="ThemeMode.Light" /> is the currently selected theme.</summary>
    public bool IsLightThemeSelected => selectedTheme == ThemeMode.Light;

    /// <summary>Gets a value indicating whether <see cref="ThemeMode.Dark" /> is the currently selected theme.</summary>
    public bool IsDarkThemeSelected => selectedTheme == ThemeMode.Dark;

    /// <summary>Gets a value indicating whether <see cref="ThemeMode.Hacker" /> is the currently selected theme.</summary>
    public bool IsHackerThemeSelected => selectedTheme == ThemeMode.Hacker;

    /// <summary>
    ///     Gets a value indicating whether a scrape command is currently running. Drives whether
    ///     <see cref="CancelCommand" /> can execute.
    /// </summary>
    public bool IsBusy
    {
        get => isBusy;
        private set => this.RaiseAndSetIfChanged(ref isBusy, value);
    }

    public string StatusText
    {
        get => statusText;
        private set => this.RaiseAndSetIfChanged(ref statusText, value);
    }

    /// <summary>
    ///     Gets or sets the category name of the most recently published thumbnail, shown as a heading below the
    ///     thumbnail preview.
    /// </summary>
    public string ThumbnailCategoryName
    {
        get => thumbnailCategoryName;
        set => this.RaiseAndSetIfChanged(ref thumbnailCategoryName, value);
    }

    /// <summary>
    ///     Gets or sets the display string of the tags kept for the most recently published thumbnail, shown below
    ///     the category name.
    /// </summary>
    public string ThumbnailTags
    {
        get => thumbnailTags;
        set => this.RaiseAndSetIfChanged(ref thumbnailTags, value);
    }

    private void SelectTheme(ThemeMode themeMode)
    {
        themeService.ApplyTheme(themeMode);
        SetSelectedTheme(themeMode);
    }

    private void SetSelectedTheme(ThemeMode themeMode)
    {
        selectedTheme = themeMode;
        this.RaisePropertyChanged(nameof(IsSystemThemeSelected));
        this.RaisePropertyChanged(nameof(IsLightThemeSelected));
        this.RaisePropertyChanged(nameof(IsDarkThemeSelected));
        this.RaisePropertyChanged(nameof(IsHackerThemeSelected));
    }

    private void CancelRunningScrape() => scrapeCancellationSource?.Cancel();

    private void AppendStatusLine(string line)
    {
        statusLines.Enqueue(line);

        while (statusLines.Count > MaxStatusLines)
        {
            statusLines.Dequeue();
        }

        StatusText = string.Join(Environment.NewLine, statusLines) + Environment.NewLine;
    }

    private ReactiveCommand<Unit, Unit> CreateOpenEditorCommand(Func<EntityEditorViewModelBase> createEditor) =>
        ReactiveCommand.CreateFromTask(async () => await OpenEditor.Handle(createEditor()));

    private ReactiveCommand<Unit, Unit> CreateResetDatabaseAndDirectoriesCommand()
    {
        var command = ReactiveCommand.CreateFromTask(ResetDatabaseAndDirectoriesAsync);

        command.ThrownExceptions.Subscribe(exception =>
            AppendStatusLine($"Reset Database and Directories: Unexpected error - {exception.Message}"));

        return command;
    }

    private async Task ResetDatabaseAndDirectoriesAsync()
    {
        if (await ConfirmScrape.Handle("Reset the database?"))
        {
            await databaseResetService.ResetDatabaseAsync(CancellationToken.None);
            AppendStatusLine("Reset Database and Directories: Database reset.");
        }

        if (await ConfirmScrape.Handle("Remove all downloaded files?"))
        {
            await databaseResetService.RemoveDownloadedFilesAsync(CancellationToken.None);
            AppendStatusLine("Reset Database and Directories: Downloaded files removed.");
        }
    }

    private ReactiveCommand<Unit, Unit> CreateScrapeCommand(string actionName, IScrapeAction? action = null)
    {
        var canExecute = this.WhenAnyValue(vm => vm.IsBusy).Select(busy => !busy);

        var command = ReactiveCommand.CreateFromTask(() => ExecuteScrapeAsync(actionName, action), canExecute);

        command.ThrownExceptions.Subscribe(exception =>
            AppendStatusLine($"{actionName}: Unexpected error - {exception.Message}"));

        return command;
    }

    private async Task ExecuteScrapeAsync(string actionName, IScrapeAction? action)
    {
        IsBusy = true;
        using var cancellationSource = new CancellationTokenSource();
        scrapeCancellationSource = cancellationSource;

        try
        {
            await ConfirmAndRunAsync(actionName, action, cancellationSource.Token);
        }
        finally
        {
            scrapeCancellationSource = null;
            IsBusy = false;
        }
    }

    private async Task ConfirmAndRunAsync(string actionName, IScrapeAction? action, CancellationToken cancellationToken)
    {
        bool confirmed = await ConfirmScrape.Handle($"Are you sure you want to start the '{actionName}'?");

        if (!confirmed)
        {
            AppendStatusLine($"{actionName}: No");

            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendStatusLine($"{actionName}: Yes");
            await RunActionAsync(actionName, action, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            AppendStatusLine($"{actionName}: Cancelled");
        }
    }

    private async Task RunActionAsync(string actionName, IScrapeAction? action, CancellationToken cancellationToken)
    {
        var page = await playwrightService.ConfigurePlaywrightAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        await page.MatchAsync(
            configuredPage => RunOnConfiguredPageAsync(actionName, action, configuredPage, cancellationToken),
            error => ReportConfigurationError(error, cancellationToken));
    }

    private async Task<FunctionalParadigm.Unit> RunOnConfiguredPageAsync(string actionName, IScrapeAction? action, IPage configuredPage, CancellationToken cancellationToken)
    {
        AppendStatusLine("Playwright page configured successfully.");

        if (action is null)
        {
            _ = configuredPage.GotoAsync("login");

            return FunctionalParadigm.Unit.Instance;
        }

        _ = configuredPage.GotoAsync("/");
        var progress = new Progress<string>(AppendStatusLine);

        return await action.ExecuteAsync(configuredPage, progress, cancellationToken)
            .MatchAsync(_ => FunctionalParadigm.Unit.Instance, error => ReportActionError(actionName, error, cancellationToken));
    }

    private FunctionalParadigm.Unit ReportActionError(string actionName, Exception error, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            AppendStatusLine($"{actionName}: {error.Message}");
        }

        return FunctionalParadigm.Unit.Instance;
    }

    private FunctionalParadigm.Unit ReportConfigurationError(Exception error, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            AppendStatusLine($"Error configuring Playwright page: {error.Message}");
        }

        return FunctionalParadigm.Unit.Instance;
    }

    public ReactiveCommand<Unit, Unit> ExitCommand { get; } = ReactiveCommand.Create(static () =>
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    });

    /// <summary>
    ///     The version CI stamps from the release tag (-p:Version=...), so the title can
    ///     never drift from the Velopack package version. SourceLink appends +sha; strip it.
    /// </summary>
    public static string ApplicationVersion { get; } = typeof(MainWindowViewModel).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion.Split('+')[0] ?? "0.0.0";

    /// <summary>
    ///     Cancels any in-flight scrape operation so it does not continue running after the view model is torn down.
    /// </summary>
    public void Dispose() => scrapeCancellationSource?.Cancel();
}
