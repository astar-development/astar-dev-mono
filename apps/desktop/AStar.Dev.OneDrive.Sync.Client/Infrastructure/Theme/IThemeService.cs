namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;

public interface IThemeService
{
    /// <summary>
    /// Gets the current application theme.
    /// </summary>
    /// </summary>
    AppTheme CurrentTheme { get; }

    /// <summary>
    /// Applies the specified theme to the application and raises the ThemeChanged event.
    /// Consumers should subscribe to ThemeChanged to update their UI when the theme changes.
    /// </summary>
    /// <param name="theme"></param>
    void Apply(AppTheme theme);

    /// <summary>
    /// Event raised whenever the application theme changes. Provides the new theme as an argument.
    /// Consumers should handle this event to update their UI accordingly.
    /// </summary>
    event EventHandler<AppTheme>? ThemeChanged;
}
