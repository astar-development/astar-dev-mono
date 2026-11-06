using AStarDev.FunctionalParadigm;
using Fab4Kids.Web.Newsletter;
using Fab4Kids.Web.TestsUnit.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Fab4Kids.Web.TestsUnit.Newsletter;

public class GivenANewsletterSubscriptionService
{
    private readonly INewsletterSubscriberStore subscriberStore = Substitute.For<INewsletterSubscriberStore>();
    private readonly INewsletterEmailSender emailSender = Substitute.For<INewsletterEmailSender>();
    private readonly FakeTimeProvider timeProvider = new();
    private readonly ILogger<NewsletterSubscriptionService> logger = LoggedEvents.CreateEnabledLogger<NewsletterSubscriptionService>();

    public GivenANewsletterSubscriptionService()
    {
        subscriberStore.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        subscriberStore.AddAsync(Arg.Any<NewsletterSubscriber>(), Arg.Any<CancellationToken>()).Returns(Unit.Instance);
        emailSender.SendAsync(Arg.Any<NewsletterSubscriber>(), Arg.Any<CancellationToken>()).Returns(Unit.Instance);
    }

    private NewsletterSubscriptionService CreateSut() => new(subscriberStore, emailSender, timeProvider, logger);

    [Fact]
    public async Task when_consent_is_not_given_then_the_outcome_is_no_consent_and_nothing_is_stored()
    {
        var sut = CreateSut();

        var outcome = await sut.SubscribeAsync("ada@example.com", consent: false, TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<NewsletterSubscriptionNoConsent>();
        await subscriberStore.DidNotReceive().AddAsync(Arg.Any<NewsletterSubscriber>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_email_is_invalid_then_the_outcome_carries_the_validation_errors()
    {
        var sut = CreateSut();

        var outcome = await sut.SubscribeAsync("not-an-email", consent: true, TestContext.Current.CancellationToken);

        var validationFailed = outcome.ShouldBeOfType<NewsletterSubscriptionValidationFailed>();
        validationFailed.Errors.ShouldContain(e => e.Property == "email");
    }

    [Fact]
    public async Task when_the_email_is_already_subscribed_then_the_outcome_is_already_subscribed_and_nothing_is_added()
    {
        subscriberStore.ExistsAsync("ada@example.com", Arg.Any<CancellationToken>()).Returns(true);
        var sut = CreateSut();

        var outcome = await sut.SubscribeAsync("ada@example.com", consent: true, TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<NewsletterSubscriptionAlreadySubscribed>();
        await subscriberStore.DidNotReceive().AddAsync(Arg.Any<NewsletterSubscriber>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_email_is_new_and_valid_then_it_is_stored_and_a_confirmation_email_is_sent()
    {
        var sut = CreateSut();

        var outcome = await sut.SubscribeAsync("ada@example.com", consent: true, TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<NewsletterSubscriptionSucceeded>();
        await subscriberStore.Received(1).AddAsync(Arg.Is<NewsletterSubscriber>(s => s.Email == "ada@example.com"), Arg.Any<CancellationToken>());
        await emailSender.Received(1).SendAsync(Arg.Is<NewsletterSubscriber>(s => s.Email == "ada@example.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_checking_for_an_existing_subscriber_fails_then_the_outcome_carries_the_failure_message()
    {
        subscriberStore.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("Something went wrong checking your subscription.");
        var sut = CreateSut();

        var outcome = await sut.SubscribeAsync("ada@example.com", consent: true, TestContext.Current.CancellationToken);

        var subscribeFailed = outcome.ShouldBeOfType<NewsletterSubscriptionSubscribeFailed>();
        subscribeFailed.Message.ShouldBe("Something went wrong checking your subscription.");
    }

    [Fact]
    public async Task when_storing_the_subscriber_fails_then_the_outcome_carries_the_failure_message()
    {
        subscriberStore.AddAsync(Arg.Any<NewsletterSubscriber>(), Arg.Any<CancellationToken>()).Returns("Something went wrong saving your subscription.");
        var sut = CreateSut();

        var outcome = await sut.SubscribeAsync("ada@example.com", consent: true, TestContext.Current.CancellationToken);

        var subscribeFailed = outcome.ShouldBeOfType<NewsletterSubscriptionSubscribeFailed>();
        subscribeFailed.Message.ShouldBe("Something went wrong saving your subscription.");
    }

    [Fact]
    public async Task when_sending_the_confirmation_email_fails_then_the_subscription_still_succeeds()
    {
        emailSender.SendAsync(Arg.Any<NewsletterSubscriber>(), Arg.Any<CancellationToken>()).Returns("Something went wrong sending your confirmation email.");
        var sut = CreateSut();

        var outcome = await sut.SubscribeAsync("ada@example.com", consent: true, TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<NewsletterSubscriptionSucceeded>();
    }

    [Fact]
    public async Task when_checking_for_an_existing_subscriber_fails_then_a_dedicated_error_carries_the_reason()
    {
        subscriberStore.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("Something went wrong checking your subscription.");
        var sut = CreateSut();

        await sut.SubscribeAsync("ada@example.com", consent: true, TestContext.Current.CancellationToken);

        logger.ValuesOf("ExistsCheckFailed")["Reason"].ShouldBe("Something went wrong checking your subscription.");
    }

    [Fact]
    public async Task when_storing_the_subscriber_fails_then_a_dedicated_error_carries_the_reason()
    {
        subscriberStore.AddAsync(Arg.Any<NewsletterSubscriber>(), Arg.Any<CancellationToken>()).Returns("Something went wrong saving your subscription.");
        var sut = CreateSut();

        await sut.SubscribeAsync("ada@example.com", consent: true, TestContext.Current.CancellationToken);

        logger.ValuesOf("SubscribeFailed")["Reason"].ShouldBe("Something went wrong saving your subscription.");
    }

    [Fact]
    public async Task when_sending_the_confirmation_email_fails_then_a_dedicated_error_carries_the_reason()
    {
        emailSender.SendAsync(Arg.Any<NewsletterSubscriber>(), Arg.Any<CancellationToken>()).Returns("Something went wrong sending your confirmation email.");
        var sut = CreateSut();

        await sut.SubscribeAsync("ada@example.com", consent: true, TestContext.Current.CancellationToken);

        logger.ValuesOf("ConfirmationEmailFailed")["Reason"].ShouldBe("Something went wrong sending your confirmation email.");
    }
}
