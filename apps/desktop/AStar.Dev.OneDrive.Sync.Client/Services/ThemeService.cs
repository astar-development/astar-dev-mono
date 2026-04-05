using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AStar.Dev.OneDrive.Sync.Client.Services;

/// <summary>
/// Switches between Light, Dark and System themes at runtime by replacing
/// the theme resource dictionary in Application.Current.Resources.
///
/// Expects two ResourceInclude entries already present in App.axaml under
/// the keys "LightThemeInclude" and "DarkThemeInclude" — only one is active
/// at a time.  On System mode it watches
/// <see cref="Application.ActualThemeVariant"/> for OS-level changes.
/// </summary>
public sealed class ThemeService : IThemeService, IDisposable
{
    private static readonly Uri _lightUri = new("avares://AStar.Dev.OneDrive.Sync.Client/Themes/Light.axaml");
    private static readonly Uri _darkUri = new("avares://AStar.Dev.OneDrive.Sync.Client/Themes/Dark.axaml");
    private Disposable? _systemWatcher;

    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;
    public event EventHandler<AppTheme>? ThemeChanged;

    public void Apply(AppTheme theme)
    {
        CurrentTheme = theme;

        _systemWatcher?.Dispose();
        _systemWatcher = null;

        if(theme == AppTheme.System)
        {
            ApplyVariant(GetSystemIsDark() ? AppTheme.Dark : AppTheme.Light);
            WatchSystem();
        }
        else
        {
            ApplyVariant(theme);
        }

        ThemeChanged?.Invoke(this, CurrentTheme);
    }

    private static bool GetSystemIsDark()
    {
        var app = Application.Current;
        return app is not null && app.ActualThemeVariant == ThemeVariant.Dark;
    }

    private void WatchSystem()
    {
        var app = Application.Current;
        if(app is null)
            return;

        app.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        _systemWatcher = new Disposable(
            () => app.ActualThemeVariantChanged -= OnActualThemeVariantChanged);
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if(CurrentTheme != AppTheme.System)
            return;
        Dispatcher.UIThread.Post(() =>
            ApplyVariant(GetSystemIsDark() ? AppTheme.Dark : AppTheme.Light));
    }

    private static void ApplyVariant(AppTheme resolved)
    {
        var app = Application.Current;
        if(app is null)
            return;

        var targetUri = resolved == AppTheme.Dark ? _darkUri : _lightUri;
        var merged = app.Resources.MergedDictionaries;

        var existing = merged
            .OfType<ResourceInclude>()
            .FirstOrDefault(r => r.Source == _lightUri || r.Source == _darkUri);

        if(existing is not null)
            _ = merged.Remove(existing);

        merged.Add(new ResourceInclude(targetUri) { Source = targetUri });
    }

    public void Dispose() => _systemWatcher?.Dispose();

    private sealed class Disposable(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
