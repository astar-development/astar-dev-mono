using Fab4Kids.Web.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Fab4Kids.Web.Components.Common;

public partial class ThemeSwitcher : ComponentBase, IDisposable
{
    private static readonly IReadOnlyList<ThemeOption> Options =
    [
        ThemeOptionFactory.Create(Theme.Light, "\U0001F324", "Light", "Switch to light theme"),
        ThemeOptionFactory.Create(Theme.Dark, "\U0001F319", "Dark", "Switch to dark theme"),
        ThemeOptionFactory.Create(Theme.Colourful, "\U0001F3A8", "Colourful", "Switch to colourful theme"),
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
        await JsRuntime.InvokeVoidAsync("fab4kidsTheme.applyThemeAttribute", theme.ToString().ToLowerInvariant());
    }

    public void Dispose()
    {
        ThemeState.OnChange -= StateHasChanged;
        GC.SuppressFinalize(this);
    }
}
