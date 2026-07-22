namespace AStar.Dev.Wallpaper.Scraper.Theming;

/// <summary>Applies and persists the application's selected <see cref="ThemeMode" />.</summary>
public interface IThemeService
{
    /// <summary>Gets the currently applied theme.</summary>
    ThemeMode CurrentTheme { get; }

    /// <summary>Loads the persisted theme selection, if any, and applies it. Defaults to <see cref="ThemeMode.System" /> when nothing is persisted.</summary>
    void Initialize();

    /// <summary>Applies <paramref name="themeMode" /> and persists the selection for the next launch.</summary>
    /// <param name="themeMode">The theme to apply.</param>
    void ApplyTheme(ThemeMode themeMode);
}
