using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using AStar.Dev.OneDriveSync.Features.Home;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

// Disambiguate ILogger: both Serilog and Microsoft.Extensions.Logging declare ILogger.
// All usages in this file intend the MEL interface.
using MelILogger = Microsoft.Extensions.Logging.ILogger;

namespace AStar.Dev.OneDriveSync;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // async void is intentional: Avalonia's OnFrameworkInitializationCompleted is a void
    // lifecycle callback; async void is the only correct pattern for async Avalonia startup.
    public override async void OnFrameworkInitializationCompleted()
    {
        _services = BuildServiceProvider();

        var db     = _services.GetRequiredService<AppDbContext>();
        var logger = _services.GetRequiredService<ILogger<App>>();

        try
        {
            await db.Database.MigrateAsync();
            LogMigrationSucceeded(logger);
        }
        catch (Exception ex)
        {
            LogMigrationFailed(logger, ex);
            // S003 will wire the corrupt-DB recovery screen into ApplicationLifetime here.
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
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
        services.AddSingleton<MainWindowViewModel>();

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

    // Source-generated log methods — zero-allocation, structured, CA1848-compliant.
    [LoggerMessage(Level = LogLevel.Information, Message = "Database migration completed successfully")]
    private static partial void LogMigrationSucceeded(MelILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Database migration failed — routing to recovery flow")]
    private static partial void LogMigrationFailed(MelILogger logger, Exception ex);
}
