using AStar.Dev.FunctionalParadigm;

namespace Fab4Kids.Web.Newsletter;

/// <summary>Factory for the <see cref="NewsletterSubscriptionOutcome"/> discriminated union.</summary>
public static class NewsletterSubscriptionOutcomeFactory
{
    public static NewsletterSubscriptionOutcome Succeeded() => new NewsletterSubscriptionSucceeded();

    public static NewsletterSubscriptionOutcome AlreadySubscribed() => new NewsletterSubscriptionAlreadySubscribed();

    public static NewsletterSubscriptionOutcome NoConsent() => new NewsletterSubscriptionNoConsent();

    public static NewsletterSubscriptionOutcome ValidationFailed(IReadOnlyList<ValidationError> errors) => new NewsletterSubscriptionValidationFailed(errors);

    public static NewsletterSubscriptionOutcome SubscribeFailed(string message) => new NewsletterSubscriptionSubscribeFailed(message);
}
