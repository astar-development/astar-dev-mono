using AStarDev.CloudSync.Core.DisplaySettings;

namespace AStarDev.CloudSync.Domain.DisplaySettings;

/// <summary>Creates <see cref="DisplayPreferences"/>.</summary>
public static class DisplayPreferencesFactory
{
    /// <summary>Creates preferences for the given layout and theme.</summary>
    /// <param name="layout">The main window layout.</param>
    /// <param name="theme">The colour theme.</param>
    public static DisplayPreferences Create(MainLayout layout, AppTheme theme) => new(layout, theme);

    /// <summary>Creates the defaults: layout A and the dark theme.</summary>
    public static DisplayPreferences CreateDefault() => Create(MainLayoutFactory.CreateUnifiedAccounts(), AppThemeFactory.CreateDark());
}
