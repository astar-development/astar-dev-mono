namespace Fab4Kids.Web.Checkout;

/// <summary>
/// Stripe checkout settings, bound from the <c>Checkout</c> configuration section
/// (Azure App Service application settings in production).
/// </summary>
public sealed class CheckoutOptions
{
    /// <summary>Stripe secret API key. Absent until the Stripe account is provisioned.</summary>
    public string? SecretKey { get; init; }

    /// <summary>The public site URL used to build the Stripe success/cancel redirect URLs.</summary>
    public string? SiteUrl { get; init; }
}
