namespace Fab4Kids.Web.Checkout;

/// <summary>Factory for the <see cref="CheckoutSessionOutcome"/> discriminated union.</summary>
public static class CheckoutSessionOutcomeFactory
{
    public static CheckoutSessionOutcome Created(string url) => new CheckoutSessionCreated(url);

    public static CheckoutSessionOutcome CartEmpty() => new CheckoutSessionCartEmpty();

    public static CheckoutSessionOutcome Failed(string message) => new CheckoutSessionFailed(message);
}
