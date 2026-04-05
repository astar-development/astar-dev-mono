using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Services;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Localization;
using AStar.Dev.OneDrive.Sync.Client.Services.Settings;
using AStar.Dev.OneDrive.Sync.Client.Services.Startup;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client;

public partial class App : Application
{
    private const string AppName    = "AStar.Dev.OneDrive.Sync";
    public static ILocalizationService Localisation { get; private set; } = null!;
    public static IThemeService Theme { get; private set; } = null!;
    public static IAuthService Auth { get; private set; } = null!;
    public static IAccountRepository Repository { get; private set; } = null!;
    public static ISyncService SyncService { get; private set; } = null!;
    public static SyncScheduler Scheduler { get; private set; } = null!;
    public static ISettingsService AppSettings { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public static string GetPlatformUserDataDirectory(string email)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string safeEmail = string.Concat(email.Split(Path.GetInvalidFileNameChars()));
        return OperatingSystem.IsWindows()
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppName,
                safeEmail)
            : OperatingSystem.IsMacOS()
                ? Path.Combine(home, "Library", "Application Support", AppName, safeEmail)
                : Path.Combine(home, ".config", AppName, safeEmail);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;

            mainWindow.Opened += async (_, _) => await BootstrapAsync(mainWindow);

            desktop.Exit += async (_, _) =>
            {
                if(Scheduler is not null)
                    await Scheduler.DisposeAsync();

                Serilog.Log.Information("[App] Application exiting");
                await Serilog.Log.CloseAndFlushAsync();
            };
        }
    }

    private static async Task BootstrapAsync(MainWindow window)
    {
        try
        {
            var locService = new LocalizationService();
            await locService.InitialiseAsync(new CultureInfo("en-GB"));
            Localisation = locService;

            var settingsService = await SettingsService.LoadAsync();
            AppSettings = settingsService;

            var themeService = new ThemeService();
            themeService.Apply(settingsService.Current.Theme);
            Theme = themeService;

            var db = DbContextFactory.Create();
            await db.Database.MigrateAsync();
            var accountRepository = new AccountRepository(db);
            var syncRepository    = new SyncRepository(db);
            Repository = accountRepository;

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
}
