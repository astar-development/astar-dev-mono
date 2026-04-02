using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.Services;
using AStar.Dev.File.App.ViewModels;
using AStar.Dev.File.App.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog.Events;

using MelILogger = Microsoft.Extensions.Logging.ILogger;
using System.Globalization;

namespace AStar.Dev.File.App;

public partial class App : Application
{
    private const string ApplicationName = "AStar.Dev.File.App";
    private static readonly string _appVersion = typeof(App).Assembly.GetName().Version?.ToString() ?? "unknown";
    private const int LogRetentionDays = 7;
    private ServiceProvider? _services;

    public T? GetService<T>() where T : class => _services?.GetService(typeof(T)) as T;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        ConfigureSerilog();
        _services = BuildServices();

        var factory = _services.GetRequiredService<IDbContextFactory<FileAppDbContext>>();
        using var ctx = factory.CreateDbContext();
        ctx.Database.Migrate();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServices()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationName, "files.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var services = new ServiceCollection();

        services.AddDbContextFactory<FileAppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<IFileTypeClassifier, FileTypeClassifier>();
        services.AddSingleton<IFolderPickerService, FolderPickerService>();
        services.AddSingleton<IFileDeleteService, FileDeleteService>();
        services.AddTransient<IFileScannerService, FileScannerService>();
        services.AddTransient<IFileViewerService, FileViewerService>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DeletePendingViewModel>();
        _ = services.AddLogging(logging => logging.AddSerilog(dispose: true));

        var serviceProvider = services.BuildServiceProvider();
        var logger          = serviceProvider.GetRequiredService<ILogger<App>>();
        LogAppStarting(logger, _appVersion);

        return serviceProvider;
    }

    private static void ConfigureSerilog()
    {
        string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationName, "logs");

        _ = Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                path: Path.Combine(logDirectory, "app.log"),
                formatProvider: CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: LogRetentionDays)
            .CreateLogger();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Application starting — version {AppVersion}")]
    private static partial void LogAppStarting(MelILogger logger, string appVersion);
}
