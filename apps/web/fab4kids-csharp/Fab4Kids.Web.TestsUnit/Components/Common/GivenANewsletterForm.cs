using AStar.Dev.FunctionalParadigm;
using Bunit;
using Fab4Kids.Web.Components.Common;
using Fab4Kids.Web.Newsletter;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.TestsUnit.Components.Common;

public class GivenANewsletterForm : Bunit.BunitContext
{
    private readonly INewsletterSubscriptionService subscriptionService = Substitute.For<INewsletterSubscriptionService>();

    public GivenANewsletterForm() => Services.AddSingleton(subscriptionService);

    [Fact]
    public async Task when_submitted_and_consent_was_not_given_then_the_no_consent_message_is_shown()
    {
        subscriptionService.SubscribeAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(NewsletterSubscriptionOutcomeFactory.NoConsent());
        var cut = Render<NewsletterForm>();
        cut.Find("input[type=email]").Input("ada@example.com");

        await cut.Find("form").SubmitAsync();

        cut.Find("p.field-error").TextContent.ShouldBe("Please check the consent box to subscribe.");
    }

    [Fact]
    public async Task when_submitted_and_the_email_is_invalid_then_the_email_error_is_shown()
    {
        ValidationError[] errors = [ValidationErrorFactory.Create("email", "Please enter a valid email address.")];
        subscriptionService.SubscribeAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(NewsletterSubscriptionOutcomeFactory.ValidationFailed(errors));
        var cut = Render<NewsletterForm>();
        cut.Find("input[type=checkbox]").Change(true);

        await cut.Find("form").SubmitAsync();

        cut.Find("p.field-error").TextContent.ShouldBe("Please enter a valid email address.");
    }

    [Fact]
    public async Task when_submitted_successfully_then_the_success_message_is_shown()
    {
        subscriptionService.SubscribeAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(NewsletterSubscriptionOutcomeFactory.Succeeded());
        var cut = Render<NewsletterForm>();
        cut.Find("input[type=email]").Input("ada@example.com");
        cut.Find("input[type=checkbox]").Change(true);

        await cut.Find("form").SubmitAsync();

        cut.Find("div.status-message--success").TextContent.ShouldContain("You're on the list");
    }

    [Fact]
    public async Task when_already_subscribed_then_the_success_message_is_still_shown()
    {
        subscriptionService.SubscribeAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(NewsletterSubscriptionOutcomeFactory.AlreadySubscribed());
        var cut = Render<NewsletterForm>();
        cut.Find("input[type=email]").Input("ada@example.com");
        cut.Find("input[type=checkbox]").Change(true);

        await cut.Find("form").SubmitAsync();

        cut.Find("div.status-message--success").TextContent.ShouldContain("You're on the list");
    }

    [Fact]
    public async Task when_subscribing_fails_then_the_failure_message_is_shown()
    {
        subscriptionService.SubscribeAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(NewsletterSubscriptionOutcomeFactory.SubscribeFailed("Something went wrong. Please try again later."));
        var cut = Render<NewsletterForm>();
        cut.Find("input[type=email]").Input("ada@example.com");
        cut.Find("input[type=checkbox]").Change(true);

        await cut.Find("form").SubmitAsync();

        cut.Find("div.status-message--error").TextContent.ShouldBe("Something went wrong. Please try again later.");
    }
}
