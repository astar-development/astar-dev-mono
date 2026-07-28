namespace Fab4Kids.Web.Fulfilment;

/// <summary>Factory for <see cref="DeliveryLink"/>.</summary>
public static class DeliveryLinkFactory
{
    public static DeliveryLink Create(string? productTitle, string url, DateTimeOffset expiresAt)
        => new(string.IsNullOrWhiteSpace(productTitle) ? "Resource" : productTitle.Trim(), url, expiresAt);
}
