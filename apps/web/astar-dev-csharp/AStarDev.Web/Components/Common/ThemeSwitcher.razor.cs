using AStarDev.Web.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AStarDev.Web.Components.Common;

public partial class ThemeSwitcher : ComponentBase, IDisposable
{
    private static readonly IReadOnlyList<ThemeOption> Options =
    [
        ThemeOptionFactory.Create(Theme.Dark, "\U0001F319", "Switch to dark theme"),
        ThemeOptionFactory.Create(Theme.Light, "☀", "Switch to light theme"),
        ThemeOptionFactory.Create(Theme.Metal, "⚡", "Switch to metal theme"),
        ThemeOptionFactory.Create(Theme.Polished, "◆", "Switch to polished theme"),
    ];

    protected override void OnInitialized() => ThemeState.OnChange += StateHasChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await ThemeState.InitializeAsync();
        StateHasChanged();
    }

    private async Task SelectAsync(Theme theme)
    {
        await ThemeState.SetThemeAsync(theme);
        await JsRuntime.InvokeVoidAsync("astarTheme.applyThemeClass", theme.ToString().ToLowerInvariant());
    }

    public void Dispose()
    {
        ThemeState.OnChange -= StateHasChanged;
        GC.SuppressFinalize(this);
    }
}
