namespace Fab4Kids.Web.Fulfilment;

/// <summary>The result of a resend-links request.</summary>
public abstract record ResendOutcome;

/// <summary>Download links were regenerated and the delivery email was resent.</summary>
public sealed record ResendSent(int LinkCount) : ResendOutcome;

/// <summary>The supplied email address did not match the order's customer email.</summary>
public sealed record ResendEmailMismatch : ResendOutcome;

/// <summary>The resend could not be completed.</summary>
public sealed record ResendFailed(string Message) : ResendOutcome;
