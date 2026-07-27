using Fab4Kids.Web.Cart;

namespace Fab4Kids.Web.Checkout;

/// <summary>Minimal API endpoints for the checkout flow.</summary>
public static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this WebApplication app) =>
        app.MapPost("/api/checkout", async (CartItem[] items, ICheckoutSessionService checkoutSessionService, CancellationToken cancellationToken) =>
        {
            var outcome = await checkoutSessionService.CreateSessionAsync(items, cancellationToken);

            return outcome switch
            {
                CheckoutSessionCreated created => Results.Ok(new { url = created.Url }),
                CheckoutSessionCartEmpty => Results.BadRequest(new { error = "Cart is empty" }),
                CheckoutSessionFailed failed => Results.Problem(failed.Message, statusCode: StatusCodes.Status500InternalServerError),
                _ => Results.Problem("Unexpected checkout outcome.", statusCode: StatusCodes.Status500InternalServerError)
            };
        });
}
