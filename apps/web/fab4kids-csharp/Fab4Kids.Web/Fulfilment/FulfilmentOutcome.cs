namespace Fab4Kids.Web.Fulfilment;

/// <summary>The result of processing a Stripe <c>checkout.session.completed</c> event.</summary>
public abstract record FulfilmentOutcome;

/// <summary>Download links were generated and the delivery email was sent.</summary>
public sealed record FulfilmentDelivered(int LinkCount) : FulfilmentOutcome;

/// <summary>The session had already been processed; no action was taken.</summary>
public sealed record FulfilmentDuplicate : FulfilmentOutcome;

/// <summary>The session had no customer email address to deliver to.</summary>
public sealed record FulfilmentNoCustomerEmail : FulfilmentOutcome;

/// <summary>Fulfilment could not be completed.</summary>
public sealed record FulfilmentFailed(string Message) : FulfilmentOutcome;
