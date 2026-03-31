using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using AStar.Dev.OneDriveSync.Features.Home;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Infrastructure.Shell;
using AStar.Dev.OneDriveSync.Infrastructure.Startup;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

// Disambiguate ILogger: both Serilog and Microsoft.Extensions.Logging declare ILogger.
// All usages in this file intend the MEL interface.
using MelILogger = Microsoft.Extensions.Logging.ILogger;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDriveSync;

public partial class App : Application, IDisposable
{
    // Cached at startup so the log call does not invoke reflection each time
    private static readonly string _appVersion =
        typeof(App).Assembly.GetName().Version?.ToString() ?? "unknown";

    private ServiceProvider?         _services;
    private CancellationTokenSource? _appLifetimeCts;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    // Avalonia event handler — async void required
    public override async void OnFrameworkInitializationCompleted()
    {
        _appLifetimeCts = new CancellationTokenSource();
        _services       = BuildServiceProvider();

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        DisableAvaloniaDataAnnotationValidation();

        var themeService = _services.GetRequiredService<IThemeService>();
        await themeService.InitialiseAsync(_appLifetimeCts.Token).ConfigureAwait(false);

        var localisationService = _services.GetRequiredService<ILocalisationService>();
        await localisationService.InitialiseAsync(_appLifetimeCts.Token).ConfigureAwait(false);
        LocalisationServiceLocator.Instance = localisationService;

        var viewModel  = _services.GetRequiredService<MainWindowViewModel>();
        var mainWindow = new MainWindow { DataContext = viewModel };

        desktop.MainWindow = mainWindow;

        desktop.ShutdownRequested += (_, _) => _appLifetimeCts.Cancel();

        base.OnFrameworkInitializationCompleted();

        await RunStartupTasksAsync(viewModel, _appLifetimeCts.Token);
    }

    public void Dispose()
    {
        _appLifetimeCts?.Cancel();
        _appLifetimeCts?.Dispose();
        _services?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task RunStartupTasksAsync(MainWindowViewModel viewModel, CancellationToken ct)
    {
        var services             = _services!;
        var logger               = services.GetRequiredService<ILogger<App>>();
        var orchestrator         = services.GetRequiredService<StartupOrchestrator>();
        var localisationService  = services.GetRequiredService<ILocalisationService>();

        LogAppStarting(logger, _appVersion);

        var results = await orchestrator.RunAsync(ct).ConfigureAwait(false);

        if (MigrationFailed(results))
            viewModel.SetStartupError(localisationService.GetString("Errors_DatabaseCorrupt"));

        viewModel.CompleteStartup();
    }

    private static bool MigrationFailed(IReadOnlyList<StartupTaskResult> results) =>
        results.Any(r => r.TaskName == DatabaseMigrationStartupTask.TaskName && !r.Succeeded);

    private static ServiceProvider BuildServiceProvider()
    {
        ConfigureSerilog();

        var services = new ServiceCollection();

        _ = services.AddLogging(logging => logging.AddSerilog(dispose: true));
        _ = services.AddPersistence();
        _ = services.AddShell();
        _ = services.AddStartupTasks();

        return services.BuildServiceProvider();
    }

    private static void ConfigureSerilog()
    {
        string logDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).CombinePath("AStar.Dev.OneDriveSync", "logs");

        _ = Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                path: Path.Combine(logDirectory, "app.log"),
                formatProvider: CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            _ = BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Application starting — version {AppVersion}")]
    private static partial void LogAppStarting(MelILogger logger, string appVersion);
}
