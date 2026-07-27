namespace Fab4Kids.Web.Checkout;

/// <summary>The result of attempting to create a Stripe checkout session.</summary>
public abstract record CheckoutSessionOutcome;

/// <summary>The session was created — the visitor should be redirected to <see cref="Url"/>.</summary>
public sealed record CheckoutSessionCreated(string Url) : CheckoutSessionOutcome;

/// <summary>The cart contained no items.</summary>
public sealed record CheckoutSessionCartEmpty : CheckoutSessionOutcome;

/// <summary>Stripe checkout is not configured, or session creation failed.</summary>
public sealed record CheckoutSessionFailed(string Message) : CheckoutSessionOutcome;
