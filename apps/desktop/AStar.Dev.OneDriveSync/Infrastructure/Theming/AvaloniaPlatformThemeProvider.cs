using System.Reactive.Subjects;
using Avalonia;
using Avalonia.Platform;

namespace AStar.Dev.OneDriveSync.Infrastructure.Theming;

/// <summary>
///     Observes OS dark/light theme changes via <see cref="IPlatformSettings.ColorValuesChanged" />.
/// </summary>
internal sealed class AvaloniaPlatformThemeProvider : IPlatformThemeProvider, IDisposable
{
    private readonly Subject<bool> _darkModeChanged = new();

    public AvaloniaPlatformThemeProvider()
    {
        if (Application.Current?.PlatformSettings is { } ps)
            ps.ColorValuesChanged += OnColorValuesChanged;
    }

    /// <inheritdoc />
    public bool IsDarkMode =>
        Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark;

    /// <inheritdoc />
    public IObservable<bool> DarkModeChanged => _darkModeChanged;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Application.Current?.PlatformSettings is { } ps)
            ps.ColorValuesChanged -= OnColorValuesChanged;

        _darkModeChanged.Dispose();
    }

    private void OnColorValuesChanged(object? sender, PlatformColorValues e) =>
        _darkModeChanged.OnNext(e.ThemeVariant == PlatformThemeVariant.Dark);
}
