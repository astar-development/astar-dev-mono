using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Fab4Kids.Web.Components.Common;

public partial class CookieBanner : ComponentBase, IDisposable
{
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
        ConsentState.OnChange -= StateHasChanged;
        GC.SuppressFinalize(this);
    }
}
