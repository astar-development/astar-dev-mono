using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AStarDev.Web.Components.Common;

public partial class CookieConsent : ComponentBase, IDisposable
{
    private string liveText = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        ConsentState.OnChange += StateHasChanged;
        await ConsentState.InitializeAsync();
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
        ConsentState.OnChange -= StateHasChanged;
        GC.SuppressFinalize(this);
    }
}
