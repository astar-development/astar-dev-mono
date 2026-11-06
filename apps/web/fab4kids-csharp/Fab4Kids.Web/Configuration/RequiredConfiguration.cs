using Fab4Kids.Web.Checkout;
using Fab4Kids.Web.Fulfilment;
using Fab4Kids.Web.Newsletter;

namespace Fab4Kids.Web.Configuration;

/// <summary>Checks the required configuration once at startup, so a missing key is reported once instead of on every request.</summary>
public static class RequiredConfiguration
{
    /// <summary>Returns the features whose required configuration values are missing or blank.</summary>
    /// <param name="checkout">The checkout options, if configured.</param>
    /// <param name="fulfilment">The fulfilment options, if configured.</param>
    /// <param name="newsletter">The newsletter options, if configured.</param>
    /// <returns>The names of the features that cannot work, in a fixed order.</returns>
    public static IReadOnlyList<string> MissingFeatures(CheckoutOptions? checkout, FulfilmentOptions? fulfilment, NewsletterOptions? newsletter)
    {
        (string Feature, string?[] Values)[] features =
        [
            ("Checkout", [checkout?.SecretKey, checkout?.SiteUrl]),
            ("Fulfilment webhook", [fulfilment?.WebhookSecret]),
            ("Fulfilment storage", [fulfilment?.StorageConnectionString, fulfilment?.BlobContainerName, fulfilment?.IdempotencyTableName]),
            ("Fulfilment email", [fulfilment?.EmailConnectionString, fulfilment?.FromAddress]),
            ("Newsletter storage", [newsletter?.TableStorageConnectionString, newsletter?.TableName]),
            ("Newsletter email", [newsletter?.EmailConnectionString, newsletter?.FromAddress]),
        ];

        return [.. features.Where(feature => feature.Values.Any(string.IsNullOrWhiteSpace)).Select(feature => feature.Feature)];
    }

    /// <summary>Logs one entry per missing feature: Critical in Production, where the feature must work, and Warning elsewhere.</summary>
    /// <param name="logger">The logger to report to.</param>
    /// <param name="environment">The host environment, which sets the level.</param>
    /// <param name="missingFeatures">The features from <see cref="MissingFeatures" />.</param>
    public static void Report(ILogger logger, IHostEnvironment environment, IReadOnlyList<string> missingFeatures)
    {
        var level = environment.IsProduction() ? LogLevel.Critical : LogLevel.Warning;
        foreach (string feature in missingFeatures)
            ConfigurationLog.RequiredConfigurationMissing(logger, level, feature);
    }
}
