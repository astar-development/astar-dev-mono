using Fab4Kids.Web.Checkout;
using Fab4Kids.Web.Configuration;
using Fab4Kids.Web.Fulfilment;
using Fab4Kids.Web.Newsletter;
using Fab4Kids.Web.TestsUnit.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;

namespace Fab4Kids.Web.TestsUnit.Configuration;

public class GivenTheRequiredConfiguration
{
    private static readonly CheckoutOptions FullCheckout = new() { SecretKey = "sk", SiteUrl = "https://fab-4-kids.co.uk" };
    private static readonly FulfilmentOptions FullFulfilment = new() { WebhookSecret = "wh", StorageConnectionString = "cs", BlobContainerName = "pdfs", IdempotencyTableName = "idem", EmailConnectionString = "ecs", FromAddress = "shop@fab-4-kids.co.uk" };
    private static readonly NewsletterOptions FullNewsletter = new() { TableStorageConnectionString = "cs", TableName = "subs", EmailConnectionString = "ecs", FromAddress = "news@fab-4-kids.co.uk" };

    [Fact]
    public void when_every_section_is_configured_then_nothing_is_missing()
        => RequiredConfiguration.MissingFeatures(FullCheckout, FullFulfilment, FullNewsletter).ShouldBeEmpty();

    [Fact]
    public void when_no_section_is_configured_then_every_feature_is_missing()
        => RequiredConfiguration.MissingFeatures(null, null, null).ShouldBe(["Checkout", "Fulfilment webhook", "Fulfilment storage", "Fulfilment email", "Newsletter storage", "Newsletter email"]);

    [Fact]
    public void when_one_value_of_a_feature_is_blank_then_only_that_feature_is_missing()
        => RequiredConfiguration.MissingFeatures(new CheckoutOptions { SecretKey = "sk", SiteUrl = " " }, FullFulfilment, FullNewsletter).ShouldBe(["Checkout"]);

    [Theory]
    [InlineData("Production", LogLevel.Critical)]
    [InlineData("Development", LogLevel.Warning)]
    public void when_features_are_missing_then_one_entry_per_feature_is_logged_at_the_environment_level(string environmentName, LogLevel expectedLevel)
    {
        var logger = LoggedEvents.CreateEnabledLogger<GivenTheRequiredConfiguration>();

        RequiredConfiguration.Report(logger, new HostingEnvironment { EnvironmentName = environmentName }, ["Checkout", "Newsletter email"]);

        logger.LevelsOf("RequiredConfigurationMissing").ShouldBe([expectedLevel, expectedLevel]);
    }

    [Fact]
    public void when_nothing_is_missing_then_nothing_is_logged()
    {
        var logger = LoggedEvents.CreateEnabledLogger<GivenTheRequiredConfiguration>();

        RequiredConfiguration.Report(logger, new HostingEnvironment { EnvironmentName = Environments.Production }, []);

        logger.LevelsOf("RequiredConfigurationMissing").ShouldBeEmpty();
    }
}
