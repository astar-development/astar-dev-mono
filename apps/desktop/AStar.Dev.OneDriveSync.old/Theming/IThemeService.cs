using Avalonia.Styling;

namespace AStar.Dev.OneDriveSync.old.Theming;

public interface IThemeService
{
    ThemeMode CurrentMode { get; }
    ThemeVariant ToVariant(ThemeMode mode);
    void Apply(ThemeMode mode);
}
