using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;

/// <summary>
/// Switches between Light, Dark, System and Hacker themes at runtime by replacing
/// the theme resource dictionary in Application.Current.Resources.
/// </summary>
public sealed class ThemeService : IThemeService, IDisposable
{
    private static readonly Uri _lightUri  = new("avares://AStar.Dev.OneDrive.Sync.Client/Themes/Light.axaml");
    private static readonly Uri _darkUri   = new("avares://AStar.Dev.OneDrive.Sync.Client/Themes/Dark.axaml");
    private static readonly Uri _hackerUri = new("avares://AStar.Dev.OneDrive.Sync.Client/Themes/Hacker.axaml");
    private Disposable? _systemWatcher;

    ///<inheritdoc />
    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    ///<inheritdoc />
    public event EventHandler<AppTheme>? ThemeChanged;

    ///<inheritdoc />
    public void Apply(AppTheme theme)
    {
        _systemWatcher?.Dispose();
        _systemWatcher = null;
        CurrentTheme = theme;
        ThemeChanged?.Invoke(this, CurrentTheme);

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ApplyVariantsOnUiThread(theme));

            return;
        }

        ApplyVariantsOnUiThread(theme);
    }

    private void ApplyVariantsOnUiThread(AppTheme theme)
    {
        if (theme == AppTheme.System)
        {
            ApplyVariant(GetSystemIsDark() ? AppTheme.Dark : AppTheme.Light);
            WatchSystem();
        }
        else
        {
            ApplyVariant(theme);
        }
    }

    private static bool GetSystemIsDark()
    {
        var app = Application.Current;

        return app is not null && app.ActualThemeVariant == ThemeVariant.Dark;
    }

    private void WatchSystem()
    {
        var app = Application.Current;
        if (app is null)
            return;

        app.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        _systemWatcher = new Disposable(
            () => app.ActualThemeVariantChanged -= OnActualThemeVariantChanged);
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (CurrentTheme != AppTheme.System)
            return;
        Dispatcher.UIThread.Post(() =>
            ApplyVariant(GetSystemIsDark() ? AppTheme.Dark : AppTheme.Light));
    }

    private static void ApplyVariant(AppTheme resolved)
    {
        var app = Application.Current;
        if (app is null)
            return;

        var targetUri = ResolveUri(resolved);
        var merged = app.Resources.MergedDictionaries;
        var existing = merged
            .OfType<ResourceInclude>()
            .FirstOrDefault(r => r.Source == _lightUri || r.Source == _darkUri || r.Source == _hackerUri);

        if (existing is not null)
            _ = merged.Remove(existing);

        merged.Add(new ResourceInclude(targetUri) { Source = targetUri });
    }

    private static Uri ResolveUri(AppTheme resolved) => resolved switch
    {
        AppTheme.Dark   => _darkUri,
        AppTheme.Hacker => _hackerUri,
        _               => _lightUri,
    };

    public void Dispose() => _systemWatcher?.Dispose();

    private sealed class Disposable(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
