namespace Fab4Kids.Web.Fulfilment;

/// <summary>Request body for <c>POST /api/resend-links</c>.</summary>
public sealed record ResendLinksRequest(string? OrderReference, string? Email);
