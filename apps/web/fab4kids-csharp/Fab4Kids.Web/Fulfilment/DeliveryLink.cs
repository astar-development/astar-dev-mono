namespace Fab4Kids.Web.Fulfilment;

/// <summary>A single signed download link included in a delivery or resend-links email.</summary>
public sealed record DeliveryLink(string ProductTitle, string Url, DateTimeOffset ExpiresAt);
