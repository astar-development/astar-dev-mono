using AStar.Dev.FunctionalParadigm;
using Fab4Kids.Web.Fulfilment;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Stripe;
using Stripe.Checkout;

namespace Fab4Kids.Web.TestsUnit.Fulfilment;

public class GivenAFulfilmentService
{
    private readonly IIdempotencyStore idempotencyStore = Substitute.For<IIdempotencyStore>();
    private readonly IPdfDeliveryLinkGenerator linkGenerator = Substitute.For<IPdfDeliveryLinkGenerator>();
    private readonly IDeliveryEmailSender emailSender = Substitute.For<IDeliveryEmailSender>();
    private readonly SessionService sessionService = Substitute.For<SessionService>();
    private readonly ILogger<FulfilmentService> logger = Substitute.For<ILogger<FulfilmentService>>();

    public GivenAFulfilmentService()
    {
        idempotencyStore.TryMarkProcessedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Success<bool, string>(true));
        linkGenerator.GenerateSignedUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(Result.Success<string, string>($"https://example.blob.core.windows.net/pdfs/{callInfo.Arg<string>()}?sig=abc")));
        emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<DeliveryLink>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<UnitFp, string>(UnitFp.Instance));
    }

    private FulfilmentService CreateSut(SessionService? client) => new(logger, idempotencyStore, linkGenerator, emailSender, client);

    private static Session SessionWith(string customerEmail, params (string name, string blobPath)[] items) => new()
    {
        Id = "cs_test_123",
        CustomerDetails = string.IsNullOrEmpty(customerEmail) ? null : new SessionCustomerDetails { Email = customerEmail },
        LineItems = new StripeList<LineItem>
        {
            Data = [.. items.Select(item => new LineItem { Price = new Price { Product = new Product { Name = item.name, Metadata = new Dictionary<string, string> { ["blobPath"] = item.blobPath } } } })]
        }
    };

    [Fact]
    public async Task when_the_session_has_already_been_processed_then_a_duplicate_outcome_is_returned()
    {
        idempotencyStore.TryMarkProcessedAsync("cs_test_123", Arg.Any<CancellationToken>()).Returns(Result.Success<bool, string>(false));
        var sut = CreateSut(sessionService);

        var outcome = await sut.ProcessCheckoutCompletedAsync("cs_test_123", TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<FulfilmentDuplicate>();
        await sessionService.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_stripe_is_not_configured_then_a_failed_outcome_is_returned()
    {
        var sut = CreateSut(null);

        var outcome = await sut.ProcessCheckoutCompletedAsync("cs_test_123", TestContext.Current.CancellationToken);

        var failed = outcome.ShouldBeOfType<FulfilmentFailed>();
        failed.Message.ShouldBe("Checkout is currently unavailable.");
    }

    [Fact]
    public async Task when_the_idempotency_store_fails_then_delivery_is_still_attempted()
    {
        idempotencyStore.TryMarkProcessedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<bool, string>("storage unavailable"));
        sessionService.GetAsync("cs_test_123", Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(SessionWith("ada@example.com", ("Times Tables Pack", "pdfs/file1.pdf")));
        var sut = CreateSut(sessionService);

        var outcome = await sut.ProcessCheckoutCompletedAsync("cs_test_123", TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<FulfilmentDelivered>();
    }

    [Fact]
    public async Task when_the_session_has_no_customer_email_then_a_no_customer_email_outcome_is_returned()
    {
        sessionService.GetAsync("cs_test_123", Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(SessionWith(string.Empty));
        var sut = CreateSut(sessionService);

        var outcome = await sut.ProcessCheckoutCompletedAsync("cs_test_123", TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<FulfilmentNoCustomerEmail>();
    }

    [Fact]
    public async Task when_the_session_retrieval_throws_then_a_failed_outcome_is_returned()
    {
        sessionService.GetAsync(Arg.Any<string>(), Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns<Task<Session>>(_ => throw new StripeException("Stripe unavailable"));
        var sut = CreateSut(sessionService);

        var outcome = await sut.ProcessCheckoutCompletedAsync("cs_test_123", TestContext.Current.CancellationToken);

        var failed = outcome.ShouldBeOfType<FulfilmentFailed>();
        failed.Message.ShouldBe("Unable to retrieve checkout session.");
    }

    [Fact]
    public async Task when_a_line_item_has_no_blob_path_then_it_is_skipped()
    {
        sessionService.GetAsync("cs_test_123", Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(SessionWith("ada@example.com", ("Times Tables Pack", "pdfs/file1.pdf"), ("Missing blob", "")));
        var sut = CreateSut(sessionService);

        var outcome = await sut.ProcessCheckoutCompletedAsync("cs_test_123", TestContext.Current.CancellationToken);

        var delivered = outcome.ShouldBeOfType<FulfilmentDelivered>();
        delivered.LinkCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_delivery_succeeds_then_the_email_is_sent_with_the_generated_links()
    {
        sessionService.GetAsync("cs_test_123", Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(SessionWith("ada@example.com", ("Times Tables Pack", "pdfs/file1.pdf")));
        var sut = CreateSut(sessionService);

        var outcome = await sut.ProcessCheckoutCompletedAsync("cs_test_123", TestContext.Current.CancellationToken);

        var delivered = outcome.ShouldBeOfType<FulfilmentDelivered>();
        delivered.LinkCount.ShouldBe(1);
        await emailSender.Received(1).SendAsync("ada@example.com", "cs_test_123", Arg.Is<IReadOnlyList<DeliveryLink>>(links => links.Count == 1 && links[0].ProductTitle == "Times Tables Pack"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_sending_the_delivery_email_fails_then_a_failed_outcome_is_returned()
    {
        sessionService.GetAsync("cs_test_123", Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(SessionWith("ada@example.com", ("Times Tables Pack", "pdfs/file1.pdf")));
        emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<DeliveryLink>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UnitFp, string>("Something went wrong sending your download links."));
        var sut = CreateSut(sessionService);

        var outcome = await sut.ProcessCheckoutCompletedAsync("cs_test_123", TestContext.Current.CancellationToken);

        var failed = outcome.ShouldBeOfType<FulfilmentFailed>();
        failed.Message.ShouldBe("Something went wrong sending your download links.");
    }

    [Fact]
    public async Task when_stripe_is_not_configured_then_resend_returns_a_failed_outcome()
    {
        var sut = CreateSut(null);

        var outcome = await sut.ResendAsync("cs_test_123", "ada@example.com", TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ResendFailed>();
    }

    [Fact]
    public async Task when_the_email_does_not_match_the_session_customer_then_an_email_mismatch_outcome_is_returned()
    {
        sessionService.GetAsync("cs_test_123", Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(SessionWith("ada@example.com", ("Times Tables Pack", "pdfs/file1.pdf")));
        var sut = CreateSut(sessionService);

        var outcome = await sut.ResendAsync("cs_test_123", "wrong@example.com", TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ResendEmailMismatch>();
        await emailSender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<DeliveryLink>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_email_matches_case_insensitively_then_the_links_are_resent()
    {
        sessionService.GetAsync("cs_test_123", Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(SessionWith("Ada@Example.com", ("Times Tables Pack", "pdfs/file1.pdf")));
        var sut = CreateSut(sessionService);

        var outcome = await sut.ResendAsync("cs_test_123", "ada@example.com", TestContext.Current.CancellationToken);

        var sent = outcome.ShouldBeOfType<ResendSent>();
        sent.LinkCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_order_reference_session_cannot_be_retrieved_then_a_failed_outcome_is_returned()
    {
        sessionService.GetAsync(Arg.Any<string>(), Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns<Task<Session>>(_ => throw new StripeException("not found"));
        var sut = CreateSut(sessionService);

        var outcome = await sut.ResendAsync("cs_test_123", "ada@example.com", TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<ResendFailed>();
    }

    [Fact]
    public async Task when_the_cancellation_token_is_already_cancelled_then_process_checkout_completed_never_retrieves_the_session()
    {
        var sut = CreateSut(sessionService);
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.ProcessCheckoutCompletedAsync("cs_test_123", cancellationTokenSource.Token));

        await sessionService.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_cancellation_token_is_already_cancelled_then_resend_never_retrieves_the_session()
    {
        var sut = CreateSut(sessionService);
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.ResendAsync("cs_test_123", "ada@example.com", cancellationTokenSource.Token));

        await sessionService.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<SessionGetOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>());
    }
}
