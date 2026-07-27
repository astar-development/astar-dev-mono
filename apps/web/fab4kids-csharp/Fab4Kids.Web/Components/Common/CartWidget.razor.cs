using System.Globalization;
using Fab4Kids.Web.Cart;
using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Common;

public sealed partial class CartWidget : ComponentBase, IDisposable
{
    private static readonly CultureInfo PriceCulture = new("en-GB");

    private bool open;

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

    public void Dispose()
    {
        CartState.OnChange -= StateHasChanged;
        GC.SuppressFinalize(this);
    }
}
