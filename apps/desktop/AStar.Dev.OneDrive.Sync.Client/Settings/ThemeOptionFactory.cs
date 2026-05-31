using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Settings;

public static class ThemeOptionFactory
{
    /// <summary>Creates the localised list of ThemeOption instances.</summary>
    public static IReadOnlyList<ThemeOption> Create(ILocalizationService loc) =>
    [
        new(AppTheme.Light,  loc.GetLocal("Settings.Theme.Light")),
        new(AppTheme.Dark,   loc.GetLocal("Settings.Theme.Dark")),
        new(AppTheme.System, loc.GetLocal("Settings.Theme.System")),
        new(AppTheme.Hacker, loc.GetLocal("Settings.Theme.Hacker")),
    ];
}
