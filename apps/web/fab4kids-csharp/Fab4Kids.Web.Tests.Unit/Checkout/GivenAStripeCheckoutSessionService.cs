using AStar.Dev.FunctionalParadigm;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Checkout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Stripe;
using Stripe.Checkout;

namespace Fab4Kids.Web.Tests.Unit.Checkout;

public class GivenAStripeCheckoutSessionService
{
    private readonly SessionService sessionService = Substitute.For<SessionService>();
    private readonly ILogger<StripeCheckoutSessionService> logger = Substitute.For<ILogger<StripeCheckoutSessionService>>();

    private static readonly IReadOnlyList<CartItem> Items = [CartItemFactory.Create(1, "Times Tables Pack", 2.50m, 2, "pdfs/times-tables.pdf")];

    private StripeCheckoutSessionService CreateSut(SessionService? client, CheckoutOptions options) => new(Options.Create(options), logger, client);

    private static CheckoutOptions ConfiguredOptions() => new() { SecretKey = "sk_test_123", SiteUrl = "https://fab-4-kids.co.uk" };

    [Fact]
    public async Task when_the_cart_is_empty_then_a_cart_empty_outcome_is_returned()
    {
        var sut = CreateSut(sessionService, ConfiguredOptions());

        var outcome = await sut.CreateSessionAsync([], TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<CheckoutSessionCartEmpty>();
    }

    [Fact]
    public async Task when_stripe_is_not_configured_then_a_failed_outcome_is_returned()
    {
        var sut = CreateSut(null, ConfiguredOptions());

        var outcome = await sut.CreateSessionAsync(Items, TestContext.Current.CancellationToken);

        var failed = outcome.ShouldBeOfType<CheckoutSessionFailed>();
        failed.Message.ShouldBe("Checkout is currently unavailable.");
    }

    [Fact]
    public async Task when_the_site_url_is_missing_then_a_failed_outcome_is_returned()
    {
        var sut = CreateSut(sessionService, new CheckoutOptions { SecretKey = "sk_test_123" });

        var outcome = await sut.CreateSessionAsync(Items, TestContext.Current.CancellationToken);

        var failed = outcome.ShouldBeOfType<CheckoutSessionFailed>();
        failed.Message.ShouldBe("Checkout is currently unavailable.");
    }

    [Fact]
    public async Task when_session_creation_succeeds_then_the_session_url_is_returned()
    {
        sessionService.CreateAsync(Arg.Any<SessionCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(new Session { Id = "cs_test_123", Url = "https://checkout.stripe.com/pay/cs_test_123" });
        var sut = CreateSut(sessionService, ConfiguredOptions());

        var outcome = await sut.CreateSessionAsync(Items, TestContext.Current.CancellationToken);

        var created = outcome.ShouldBeOfType<CheckoutSessionCreated>();
        created.Url.ShouldBe("https://checkout.stripe.com/pay/cs_test_123");
    }

    [Fact]
    public async Task when_session_creation_sends_the_cart_line_items_to_stripe()
    {
        sessionService.CreateAsync(Arg.Any<SessionCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(new Session { Id = "cs_test_123", Url = "https://checkout.stripe.com/pay/cs_test_123" });
        var sut = CreateSut(sessionService, ConfiguredOptions());

        await sut.CreateSessionAsync(Items, TestContext.Current.CancellationToken);

        await sessionService.Received(1).CreateAsync(
            Arg.Is<SessionCreateOptions>(options => options.LineItems.Count == 1 && options.LineItems[0].Quantity == 2 && options.LineItems[0].PriceData.ProductData.Name == "Times Tables Pack"),
            Arg.Any<RequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_session_creation_sends_the_blob_path_as_product_metadata()
    {
        sessionService.CreateAsync(Arg.Any<SessionCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(new Session { Id = "cs_test_123", Url = "https://checkout.stripe.com/pay/cs_test_123" });
        var sut = CreateSut(sessionService, ConfiguredOptions());

        await sut.CreateSessionAsync(Items, TestContext.Current.CancellationToken);

        await sessionService.Received(1).CreateAsync(
            Arg.Is<SessionCreateOptions>(options => options.LineItems[0].PriceData.ProductData.Metadata["blobPath"] == "pdfs/times-tables.pdf"),
            Arg.Any<RequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_session_creation_throws_then_a_failed_outcome_is_returned()
    {
        sessionService.CreateAsync(Arg.Any<SessionCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns<Task<Session>>(_ => throw new StripeException("card declined"));
        var sut = CreateSut(sessionService, ConfiguredOptions());

        var outcome = await sut.CreateSessionAsync(Items, TestContext.Current.CancellationToken);

        var failed = outcome.ShouldBeOfType<CheckoutSessionFailed>();
        failed.Message.ShouldBe("Unable to create checkout session.");
    }

    [Fact]
    public async Task when_stripe_is_not_configured_then_no_customer_email_is_returned()
    {
        var sut = CreateSut(null, ConfiguredOptions());

        var email = await sut.GetCustomerEmailAsync("cs_test_123", TestContext.Current.CancellationToken);

        email.TryGetValue(out string _).ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_session_has_a_customer_email_then_it_is_returned()
    {
        sessionService.GetAsync("cs_test_123", Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(new Session { CustomerDetails = new SessionCustomerDetails { Email = "ada@example.com" } });
        var sut = CreateSut(sessionService, ConfiguredOptions());

        var email = await sut.GetCustomerEmailAsync("cs_test_123", TestContext.Current.CancellationToken);

        email.TryGetValue(out string value).ShouldBeTrue();
        value.ShouldBe("ada@example.com");
    }

    [Fact]
    public async Task when_the_session_has_no_customer_email_then_none_is_returned()
    {
        sessionService.GetAsync("cs_test_123", Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(new Session { CustomerDetails = null });
        var sut = CreateSut(sessionService, ConfiguredOptions());

        var email = await sut.GetCustomerEmailAsync("cs_test_123", TestContext.Current.CancellationToken);

        email.TryGetValue(out string _).ShouldBeFalse();
    }
}
