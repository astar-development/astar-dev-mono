using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Fab4Kids.Web.Components.Common;

public sealed partial class CookieBanner : ComponentBase, IDisposable
{
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
        await JsRuntime.InvokeVoidAsync("fab4kidsCookieConsent.notifyAccepted");
    }

    private async Task DeclineAsync() => await ConsentState.SetPreferenceAsync(analyticsAccepted: false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (disposing)
            ConsentState.OnChange -= StateHasChanged;
    }
}
