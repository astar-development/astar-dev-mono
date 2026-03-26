using Avalonia.Styling;

namespace AStar.Dev.OneDriveSync.Theming;

public sealed class ThemeService(IApplicationThemeAdapter adapter) : IThemeService
{
    public ThemeMode CurrentMode { get; private set; } = ThemeMode.Auto;

    public ThemeVariant ToVariant(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => ThemeVariant.Light,
        ThemeMode.Dark  => ThemeVariant.Dark,
        _               => ThemeVariant.Default
    };

    public void Apply(ThemeMode mode)
    {
        CurrentMode = mode;
        adapter.SetThemeVariant(ToVariant(mode));
    }
}
