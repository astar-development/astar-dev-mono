using System.Globalization;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Checkout;
using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Common;

public sealed partial class CartWidget : ComponentBase, IDisposable
{
    private static readonly CultureInfo PriceCulture = new("en-GB");

    private bool open;
    private bool checkingOut;
    private string? checkoutError;

    protected override void OnInitialized() => CartState.OnChange += StateHasChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await CartState.InitializeAsync();
        StateHasChanged();
    }

    private void Toggle() => open = !open;

    private async Task RemoveAsync(int productId) => await CartState.RemoveItemAsync(productId);

    private async Task CheckoutAsync()
    {
        checkingOut = true;
        checkoutError = null;
        StateHasChanged();

        var outcome = await CheckoutSessionService.CreateSessionAsync(CartState.Items, CancellationToken.None);
        switch (outcome)
        {
            case CheckoutSessionCreated created:
                NavigationManager.NavigateTo(created.Url, forceLoad: true);

                return;
            case CheckoutSessionCartEmpty:
                checkoutError = "Your basket is empty.";

                break;
            case CheckoutSessionFailed failed:
                checkoutError = failed.Message;

                break;
        }

        checkingOut = false;
        StateHasChanged();
    }

    public void Dispose()
    {
        CartState.OnChange -= StateHasChanged;
        GC.SuppressFinalize(this);
    }
}
