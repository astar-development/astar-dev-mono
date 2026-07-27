namespace Fab4Kids.Web.Newsletter;

/// <summary>A visitor who has opted in to the fab4kids newsletter.</summary>
public sealed record NewsletterSubscriber(string Email, DateTimeOffset SubscribedAt);
