namespace Fab4Kids.Web.Fulfilment;

/// <summary>
/// Stripe webhook, PDF delivery, and idempotency settings, bound from the
/// <c>Fulfilment</c> configuration section (Azure App Service application settings in production).
/// </summary>
public sealed class FulfilmentOptions
{
    /// <summary>Stripe webhook signing secret, used to verify the <c>Stripe-Signature</c> header.</summary>
    public string? WebhookSecret { get; init; }

    /// <summary>Azure Storage connection string, shared by the PDF blob container and the idempotency table.</summary>
    public string? StorageConnectionString { get; init; }

    /// <summary>The blob container that holds the purchasable PDF resources.</summary>
    public string? BlobContainerName { get; init; }

    /// <summary>The table that records already-processed Stripe checkout session IDs.</summary>
    public string? IdempotencyTableName { get; init; }

    /// <summary>Azure Communication Services connection string used to send delivery/resend-links emails.</summary>
    public string? EmailConnectionString { get; init; }

    /// <summary>The verified ACS sender address, e.g. <c>Orders@&lt;verified-domain&gt;</c>.</summary>
    public string? FromAddress { get; init; }
}
