using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure;
using AStar.Dev.OneDrive.Sync.Client.Splash;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.LogViewer;
using AStar.Dev.OneDrive.Sync.Client.Startup;
using AStar.Dev.Utilities;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using Microsoft.Extensions.Configuration;

namespace AStar.Dev.OneDrive.Sync.Client;

public class App : Application, IDisposable
{
    private readonly ServiceProvider _services = BuildServiceProvider();
    private readonly CancellationTokenSource _appLifetimeCts = new();
    private bool _disposed;
    private static ISyncScheduler Scheduler { get; set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var splashWindow = new SplashWindow();
        var mainWindow = _services.GetRequiredService<MainWindow>();
        desktop.MainWindow = splashWindow;

        splashWindow.Opened += async (_, _) =>
        {
            await BootstrapAsync(mainWindow, new Progress<string>(splashWindow.SetStatus));
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            splashWindow.Close();
        };

        desktop.Exit += async (_, _) =>
        {
            await Scheduler.DisposeAsync();

            Log.Information("[App] Application exiting");
            await Log.CloseAndFlushAsync();
        };
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var inMemoryLogSink = new InMemoryLogSink();
        ConfigureSerilog(inMemoryLogSink);

        var services = new ServiceCollection();

        _ = services.AddLogging(logging => logging.AddSerilog(dispose: true));
        _ = services.AddPersistence();
        _ = services.AddLocalizationServices();
        _ = services.AddStartupTasks();
        _ = services.AddViews();
        _ = services.AddViewModels();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        _ = services.AddOptions<EntraIdConfiguration>()
                .Bind(configuration.GetSection("EntraId"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        _ = services.AddShell(inMemoryLogSink);

        return services.BuildServiceProvider();
    }

    private static void ConfigureSerilog(InMemoryLogSink inMemoryLogSink)
    {
        string logDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).CombinePath(ApplicationMetadata.ApplicationName, "logs");

        _ = Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                path: Path.Combine(logDirectory, ApplicationMetadata.ApplicationLogName),
                formatProvider: CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .WriteTo.Sink(inMemoryLogSink)
            .CreateLogger();
    }

    private async Task BootstrapAsync(MainWindow window, IProgress<string> progress)
    {
        try
        {
            progress.Report("Loading settings…");
            var settingsService = await SettingsService.LoadAsync();

            progress.Report("Applying theme…");
            var themeService = _services.GetRequiredService<IThemeService>();
            themeService.Apply(settingsService.Current.Theme);

            progress.Report("Configuring services…");
            var accountRepository = _services.GetRequiredService<IAccountRepository>();
            var syncRepository = _services.GetRequiredService<ISyncRepository>();

            var authService = _services.GetRequiredService<IAuthService>();
            var localisationService = _services.GetRequiredService<ILocalizationService>();
            var graphService = _services.GetRequiredService<IGraphService>();
            var syncService = _services.GetRequiredService<ISyncService>();
            var scheduler = _services.GetRequiredService<ISyncScheduler>();
            Scheduler = scheduler;

            progress.Report("Initialising startup…");
            var startupService = _services.GetRequiredService<IStartupService>();

            progress.Report("Starting sync scheduler…");
            scheduler.StartSync(TimeSpan.FromMinutes(settingsService.Current.SyncIntervalMinutes));
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[App] Fatal error during bootstrap: {Message}", ex.Message);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _appLifetimeCts.Dispose();
        _services.Dispose();

        GC.SuppressFinalize(this);
    }
}
