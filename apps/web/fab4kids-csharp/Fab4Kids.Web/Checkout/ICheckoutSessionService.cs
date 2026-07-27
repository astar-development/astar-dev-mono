using AStar.Dev.FunctionalParadigm;
using Fab4Kids.Web.Cart;

namespace Fab4Kids.Web.Checkout;

/// <summary>Creates Stripe checkout sessions for the visitor's basket and looks up completed sessions.</summary>
public interface ICheckoutSessionService
{
    /// <summary>Creates a Stripe checkout session for the given cart items.</summary>
    Task<CheckoutSessionOutcome> CreateSessionAsync(IReadOnlyList<CartItem> items, CancellationToken cancellationToken);

    /// <summary>Looks up the customer email recorded against a completed checkout session.</summary>
    Task<Option<string>> GetCustomerEmailAsync(string sessionId, CancellationToken cancellationToken);
}
