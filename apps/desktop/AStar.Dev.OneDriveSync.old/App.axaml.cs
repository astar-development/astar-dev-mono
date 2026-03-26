using System.Globalization;
using System.Resources;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AStar.Dev.OneDriveSync.old.Localisation;
using AStar.Dev.OneDriveSync.old.Logging;
using AStar.Dev.OneDriveSync.old.Services;
using AStar.Dev.OneDriveSync.old.Theming;
using AStar.Dev.OneDriveSync.old.ViewModels;

namespace AStar.Dev.OneDriveSync.old;

#pragma warning disable CA1001 // Disposed via desktop.Exit handler in OnFrameworkInitializationCompleted
public partial class App : Application
#pragma warning restore CA1001
{
    private ThemeService? _themeService;
    private LocalisationService? _localisationService;
    private LoggingService? _loggingService;

    /// <summary>
    /// AM-01: Azure AD app-registration client ID for personal Microsoft accounts.
    /// Replace with your own registered application's client ID before first use.
    /// </summary>
    internal static string MsalClientId { get; set; } = "3057f494-687d-4abb-a653-4b8066230b6e";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _themeService = new ThemeService(new AvaloniaApplicationThemeAdapter());
        _localisationService = new LocalisationService(
            new ResxStringResourceProvider(new ResourceManager(
                "AStar.Dev.OneDriveSync.old.Localisation.Strings",
                typeof(App).Assembly)),
            CultureInfo.GetCultureInfo("en-GB"));
        _loggingService = new LoggingService();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var accountStore = new JsonAccountStore();
            var authService = new MsalAuthService(MsalClientId);
            var folderService = new GraphOneDriveFolderService();

            desktop.MainWindow = new MainWindow
            {
                // All user-facing strings loaded from the localisation service (TI-03, TI-04)
                Title       = _localisationService!.GetString("MainWindow_Title"),
                DataContext = new MainWindowViewModel(_themeService!, _loggingService!, accountStore, authService, folderService)
            };

            desktop.Exit += (_, _) => _loggingService?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
