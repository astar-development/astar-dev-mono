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

        var viewModel  = _services.GetRequiredService<MainWindowViewModel>();
        var mainWindow = new MainWindow { DataContext = viewModel };

        desktop.MainWindow = mainWindow;

        // Cancel startup tasks cleanly when the user closes the window
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
        var services     = _services!;
        var logger       = services.GetRequiredService<ILogger<App>>();
        var orchestrator = services.GetRequiredService<StartupOrchestrator>();

        LogAppStarting(logger, AppVersion);

        var results = await orchestrator.RunAsync(ct).ConfigureAwait(false);

        var migrationFailed = results.Any(
            r => r.TaskName == DatabaseMigrationStartupTask.TaskName && !r.Succeeded);

        if (migrationFailed)
        {
            // EH-08: full 'Database corrupt — Start Fresh?' recovery dialog deferred to the
            // error-handling feature story. Surface a visible error banner for now.
            viewModel.SetStartupError(
                "Database is corrupt. Please restart and choose 'Start Fresh' when prompted.");
        }

        viewModel.CompleteStartup();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AStar.Dev.OneDriveSync",
            "logs");

        Directory.CreateDirectory(logDirectory);

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

        var services = new ServiceCollection();

        services.AddLogging(logging => logging.AddSerilog(dispose: true));
        services.AddPersistence();
        services.AddShell();
        services.AddStartupTasks();

        return services.BuildServiceProvider();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Application starting — version {AppVersion}")]
    private static partial void LogAppStarting(MelILogger logger, string appVersion);
}
