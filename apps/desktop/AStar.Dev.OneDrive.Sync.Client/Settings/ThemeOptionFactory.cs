using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Settings;

public static class ThemeOptionFactory
{
    /// <summary>Creates the localised list of ThemeOption instances with <see cref="ThemeOption.IsSelected"/> set for the matching theme.</summary>
    public static IReadOnlyList<ThemeOption> Create(ILocalizationService loc, AppTheme selectedTheme) =>
    [
        new(AppTheme.Light,  loc.GetLocal("Settings.Theme.Light"),  selectedTheme == AppTheme.Light),
        new(AppTheme.Dark,   loc.GetLocal("Settings.Theme.Dark"),   selectedTheme == AppTheme.Dark),
        new(AppTheme.System, loc.GetLocal("Settings.Theme.System"), selectedTheme == AppTheme.System),
        new(AppTheme.Hacker, loc.GetLocal("Settings.Theme.Hacker"), selectedTheme == AppTheme.Hacker),
    ];
}
