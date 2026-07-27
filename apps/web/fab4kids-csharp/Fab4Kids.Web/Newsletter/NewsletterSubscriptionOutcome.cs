using AStar.Dev.FunctionalParadigm;

namespace Fab4Kids.Web.Newsletter;

/// <summary>The result of attempting to subscribe to the newsletter.</summary>
public abstract record NewsletterSubscriptionOutcome;

/// <summary>The email was validated and the subscriber record was stored.</summary>
public sealed record NewsletterSubscriptionSucceeded : NewsletterSubscriptionOutcome;

/// <summary>The email is already subscribed — the visitor should see a success message regardless.</summary>
public sealed record NewsletterSubscriptionAlreadySubscribed : NewsletterSubscriptionOutcome;

/// <summary>The visitor did not check the consent checkbox.</summary>
public sealed record NewsletterSubscriptionNoConsent : NewsletterSubscriptionOutcome;

/// <summary>The email address failed validation.</summary>
public sealed record NewsletterSubscriptionValidationFailed(IReadOnlyList<ValidationError> Errors) : NewsletterSubscriptionOutcome;

/// <summary>The email was valid but the subscription could not be stored.</summary>
public sealed record NewsletterSubscriptionSubscribeFailed(string Message) : NewsletterSubscriptionOutcome;
