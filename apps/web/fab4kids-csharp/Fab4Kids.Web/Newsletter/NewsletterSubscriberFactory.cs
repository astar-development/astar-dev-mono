namespace Fab4Kids.Web.Newsletter;

/// <summary>Factory for <see cref="NewsletterSubscriber"/>.</summary>
public static class NewsletterSubscriberFactory
{
    public static NewsletterSubscriber Create(string? email, DateTimeOffset subscribedAt)
        => new(string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant(), subscribedAt);
}
