using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using AStar.Dev.OneDriveSync.Features.Home;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using AStar.Dev.OneDriveSync.Infrastructure.Shell;
using AStar.Dev.OneDriveSync.Infrastructure.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

// Disambiguate ILogger: both Serilog and Microsoft.Extensions.Logging declare ILogger.
// All usages in this file intend the MEL interface.
using MelILogger = Microsoft.Extensions.Logging.ILogger;

namespace AStar.Dev.OneDriveSync;

public partial class App : Application, IDisposable
{
    // Cached at startup so the log call does not invoke reflection each time
    private static readonly string AppVersion =
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

        MainWindowViewModel viewModel  = _services.GetRequiredService<MainWindowViewModel>();
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
        ServiceProvider services     = _services!;
        ILogger<App> logger       = services.GetRequiredService<ILogger<App>>();
        StartupOrchestrator orchestrator = services.GetRequiredService<StartupOrchestrator>();

        LogAppStarting(logger, AppVersion);

        IReadOnlyList<StartupTaskResult> results = await orchestrator.RunAsync(ct).ConfigureAwait(false);

        if (MigrationFailed(results))
            NotifyDatabaseCorrupt(viewModel);

        viewModel.CompleteStartup();
    }

    private static bool MigrationFailed(IReadOnlyList<StartupTaskResult> results) =>
        results.Any(r => r.TaskName == DatabaseMigrationStartupTask.TaskName && !r.Succeeded);

    private static void NotifyDatabaseCorrupt(MainWindowViewModel viewModel) =>
        viewModel.SetStartupError(
            "Database is corrupt. Please restart and choose 'Start Fresh' when prompted.");

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
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AStar.Dev.OneDriveSync",
            "logs");

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
        DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (DataAnnotationsValidationPlugin? plugin in dataValidationPluginsToRemove)
        {
            _ = BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Application starting — version {AppVersion}")]
    private static partial void LogAppStarting(MelILogger logger, string appVersion);
}
