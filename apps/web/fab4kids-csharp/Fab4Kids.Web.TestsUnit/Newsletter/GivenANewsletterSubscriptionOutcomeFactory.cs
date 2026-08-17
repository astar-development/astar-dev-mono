using AStar.Dev.FunctionalParadigm;
using Fab4Kids.Web.Newsletter;

namespace Fab4Kids.Web.TestsUnit.Newsletter;

public class GivenANewsletterSubscriptionOutcomeFactory
{
    [Fact]
    public void when_succeeded_is_created_then_it_is_the_succeeded_case()
        => NewsletterSubscriptionOutcomeFactory.Succeeded().ShouldBeOfType<NewsletterSubscriptionSucceeded>();

    [Fact]
    public void when_already_subscribed_is_created_then_it_is_the_already_subscribed_case()
        => NewsletterSubscriptionOutcomeFactory.AlreadySubscribed().ShouldBeOfType<NewsletterSubscriptionAlreadySubscribed>();

    [Fact]
    public void when_no_consent_is_created_then_it_is_the_no_consent_case()
        => NewsletterSubscriptionOutcomeFactory.NoConsent().ShouldBeOfType<NewsletterSubscriptionNoConsent>();

    [Fact]
    public void when_validation_failed_is_created_then_it_carries_the_errors()
    {
        ValidationError[] errors = [ValidationErrorFactory.Create("email", "Email is required.")];

        var sut = NewsletterSubscriptionOutcomeFactory.ValidationFailed(errors);

        var validationFailed = sut.ShouldBeOfType<NewsletterSubscriptionValidationFailed>();
        validationFailed.Errors.ShouldBe(errors);
    }

    [Fact]
    public void when_subscribe_failed_is_created_then_it_carries_the_message()
    {
        var sut = NewsletterSubscriptionOutcomeFactory.SubscribeFailed("Something went wrong.");

        var subscribeFailed = sut.ShouldBeOfType<NewsletterSubscriptionSubscribeFailed>();
        subscribeFailed.Message.ShouldBe("Something went wrong.");
    }
}
