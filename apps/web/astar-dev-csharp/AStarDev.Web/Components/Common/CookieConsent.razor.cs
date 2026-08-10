using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AStarDev.Web.Components.Common;

public partial class CookieConsent : ComponentBase, IDisposable
{
    private string liveText = string.Empty;
    private bool disposed;

    protected override void OnInitialized() => ConsentState.OnChange += StateHasChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await ConsentState.InitializeAsync();
        StateHasChanged();
    }

    private async Task AcceptAsync()
    {
        await ConsentState.SetPreferenceAsync(analyticsAccepted: true);
        liveText = "Cookie preferences saved";
        await JsRuntime.InvokeVoidAsync("astarCookieConsent.notifyAccepted");
    }

    private async Task DeclineAsync()
    {
        await ConsentState.SetPreferenceAsync(analyticsAccepted: false);
        liveText = "Cookie preferences saved";
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (disposing)
            ConsentState.OnChange -= StateHasChanged;
    }
}
