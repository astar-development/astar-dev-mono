using AStar.Dev.FunctionalParadigm;
using Fab4Kids.Web.Checkout;
using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Pages;

public sealed partial class CheckoutSuccess : ComponentBase
{
    private string? customerEmail;

    [SupplyParameterFromQuery(Name = "session_id")]
    public string? SessionId { get; set; }

    [Inject]
    public required ICheckoutSessionService CheckoutSessionService { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            NavigationManager.NavigateTo("/");

            return;
        }

        var email = await CheckoutSessionService.GetCustomerEmailAsync(SessionId, CancellationToken.None);
        email.TryGetValue(out string value);
        customerEmail = value;
    }
}
