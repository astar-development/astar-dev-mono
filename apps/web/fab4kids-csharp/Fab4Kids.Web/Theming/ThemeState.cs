using Blazored.LocalStorage;

namespace Fab4Kids.Web.Theming;

/// <summary>
/// Tracks the active <see cref="Theme"/> for the current circuit and persists the
/// choice to browser local storage, mirroring the previous Astro site's
/// <c>useTheme.ts</c> behaviour.
/// </summary>
public sealed class ThemeState(ILocalStorageService localStorage)
{
    private const string StorageKey = "fab4kids-theme";

    public Theme Current { get; private set; } = Theme.Light;

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        var stored = await localStorage.GetItemAsStringAsync(StorageKey);
        if (stored is not null && Enum.TryParse<Theme>(stored, ignoreCase: true, out var parsed))
        {
            Current = parsed;
        }
    }

    public async Task SetThemeAsync(Theme theme)
    {
        Current = theme;
        await localStorage.SetItemAsStringAsync(StorageKey, theme.ToString().ToLowerInvariant());
        OnChange?.Invoke();
    }
}
