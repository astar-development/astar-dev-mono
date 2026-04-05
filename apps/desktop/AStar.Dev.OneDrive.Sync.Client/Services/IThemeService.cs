namespace AStar.Dev.OneDrive.Sync.Client.Services;

public enum AppTheme { Light, Dark, System }

public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    void Apply(AppTheme theme);
    event EventHandler<AppTheme>? ThemeChanged;
}
