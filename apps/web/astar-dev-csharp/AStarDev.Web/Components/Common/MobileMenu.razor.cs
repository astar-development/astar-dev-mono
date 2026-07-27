using AStarDev.Web.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AStarDev.Web.Components.Common;

public partial class MobileMenu : ComponentBase, IAsyncDisposable
{
    [Parameter, EditorRequired]
    public string GithubUrl { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string NugetUrl { get; set; } = string.Empty;

    private ElementReference drawerElement;
    private ElementReference triggerElement;
    private DotNetObjectReference<MobileMenu>? dotNetRef;
    private bool isOpen;

    private bool IsActive(string href) => SiteNavigation.IsActive(href, new Uri(Navigation.Uri).AbsolutePath);

    private Task ToggleAsync() => isOpen ? CloseAsync() : OpenAsync();

    private async Task OpenAsync()
    {
        isOpen = true;
        StateHasChanged();
        dotNetRef ??= DotNetObjectReference.Create(this);
        await JsRuntime.InvokeVoidAsync("astarMobileMenu.attach", drawerElement, triggerElement, dotNetRef);
    }

    private async Task CloseAsync()
    {
        isOpen = false;
        await JsRuntime.InvokeVoidAsync("astarMobileMenu.detach", triggerElement);
    }

    [JSInvokable]
    public async Task CloseFromJsAsync()
    {
        isOpen = false;
        await InvokeAsync(StateHasChanged);
        await JsRuntime.InvokeVoidAsync("astarMobileMenu.detach", triggerElement);
    }

    public async ValueTask DisposeAsync()
    {
        if (isOpen)
        {
            await JsRuntime.InvokeVoidAsync("astarMobileMenu.detach", triggerElement);
        }

        dotNetRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}
