using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.OneDrive;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Persistence;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.LogViewer;
using AStar.Dev.Utilities;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace AStar.Dev.OneDrive.Sync.Client;

public partial class App : Application, IDisposable
{
    private ServiceProvider         _services = null!;
    private readonly CancellationTokenSource _appLifetimeCts = new();
    private bool _disposed;
    public static ILocalizationService Localisation { get; private set; } = null!;
    public static IThemeService Theme { get; private set; } = null!;
    public static IAuthService Auth { get; private set; } = null!;
    public static IAccountRepository AccountRepository { get; private set; } = null!;
    public static ISyncService SyncService { get; private set; } = null!;
    public static SyncScheduler Scheduler { get; private set; } = null!;
    public static ISettingsService AppSettings { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
        _services       = BuildServiceProvider();

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var mainWindow = _services.GetRequiredService<MainWindow>();
        desktop.MainWindow = mainWindow;

        mainWindow.Opened += async (_, _) => await BootstrapAsync(mainWindow);

        desktop.Exit += async (_, _) =>
        {
            await Scheduler.DisposeAsync();

            Serilog.Log.Information("[App] Application exiting");
            await Serilog.Log.CloseAndFlushAsync();
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
        _ = services.AddShell(BuildOneDriveClientOptions(), inMemoryLogSink);
        _ = services.AddStartupTasks();
        _ = services.AddViews();
        _ = services.AddViewModels();

        return services.BuildServiceProvider();
    }

    private static OneDriveClientOptions BuildOneDriveClientOptions()
    {
        string clientId     = Environment.GetEnvironmentVariable("ONEDRIVEYNC_AZURE_CLIENT_ID") ?? "3057f494-687d-4abb-a653-4b8066230b6e";
        string redirectUri  = Environment.GetEnvironmentVariable("ONEDRIVESYNC_REDIRECT_URI") ?? "http://localhost";

        return OneDriveClientOptionsFactory.Create(clientId, new Uri(redirectUri));
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
                path: Path.Combine(logDirectory, "app.log"),
                formatProvider: CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .WriteTo.Sink(inMemoryLogSink)
            .CreateLogger();
    }

    private async Task BootstrapAsync(MainWindow window)
    {
        try
        {
            var locService = _services.GetRequiredService<ILocalizationService>();
            Localisation = locService;

            var settingsService = await SettingsService.LoadAsync();
            AppSettings = settingsService;

            var themeService = new ThemeService();
            themeService.Apply(settingsService.Current.Theme);
            Theme = themeService;

            var accountRepository = _services.GetRequiredService<IAccountRepository>();
            var syncRepository    = _services.GetRequiredService<ISyncRepository>();
            AccountRepository = accountRepository;

            var tokenCache  = new TokenCacheService();
            var authService = new AuthService(tokenCache);
            Auth = authService;

            var graphService   = new GraphService();
            var syncService    = new SyncService(authService, graphService, accountRepository, syncRepository);
            var scheduler      = new SyncScheduler(syncService, accountRepository);
            SyncService = syncService;
            Scheduler = scheduler;

            var startupService = new StartupService(accountRepository, authService);

            await window.InitialiseAsync(authService, graphService, startupService, syncService, scheduler, syncRepository, settingsService, accountRepository);

            scheduler.Start(TimeSpan.FromMinutes(settingsService.Current.SyncIntervalMinutes));
        }
        catch(Exception ex)
        {
            Serilog.Log.Fatal(ex, "[App] Fatal error during bootstrap: {Message}", ex.Message);
        }
    }

    public void Dispose()
    {
        if(_disposed)
            return;

        _disposed = true;
        _appLifetimeCts.Dispose();
        _services.Dispose();

        GC.SuppressFinalize(this);
    }
}
