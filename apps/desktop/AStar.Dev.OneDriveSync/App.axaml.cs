using System.Globalization;
using System.Resources;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AStar.Dev.OneDriveSync.Localisation;
using AStar.Dev.OneDriveSync.Theming;

namespace AStar.Dev.OneDriveSync;

public partial class App : Application
{
    private ThemeService? _themeService;
    private LocalisationService? _localisationService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _themeService = new ThemeService(new AvaloniaApplicationThemeAdapter());
        _localisationService = new LocalisationService(
            new ResxStringResourceProvider(new ResourceManager(
                "AStar.Dev.OneDriveSync.Localisation.Strings",
                typeof(App).Assembly)),
            CultureInfo.GetCultureInfo("en-GB"));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _themeService!.Apply(ThemeMode.Auto);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                // All user-facing strings loaded from the localisation service (TI-03, TI-04)
                Title = _localisationService!.GetString("MainWindow_Title")
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
