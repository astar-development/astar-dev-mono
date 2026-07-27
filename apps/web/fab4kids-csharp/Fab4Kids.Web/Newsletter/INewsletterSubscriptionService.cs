namespace Fab4Kids.Web.Newsletter;

/// <summary>Orchestrates a newsletter signup: consent check, validation, then storing and confirming.</summary>
public interface INewsletterSubscriptionService
{
    Task<NewsletterSubscriptionOutcome> SubscribeAsync(string email, bool consent, CancellationToken cancellationToken);
}
