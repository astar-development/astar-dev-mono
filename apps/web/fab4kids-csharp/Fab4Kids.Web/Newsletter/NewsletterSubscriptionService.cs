using AStarDev.FunctionalParadigm;

namespace Fab4Kids.Web.Newsletter;

/// <inheritdoc cref="INewsletterSubscriptionService"/>
public sealed class NewsletterSubscriptionService(INewsletterSubscriberStore subscriberStore, INewsletterEmailSender emailSender, TimeProvider timeProvider, ILogger<NewsletterSubscriptionService> logger) : INewsletterSubscriptionService
{
    public async Task<NewsletterSubscriptionOutcome> SubscribeAsync(string email, bool consent, CancellationToken cancellationToken)
    {
        if (!consent)
        {
            NewsletterLog.ConsentNotGiven(logger);

            return NewsletterSubscriptionOutcomeFactory.NoConsent();
        }

        var validation = NewsletterValidator.Validate(email);
        if (validation.TryGetErrors(out var errors))
        {
            NewsletterLog.InvalidEmail(logger);

            return NewsletterSubscriptionOutcomeFactory.ValidationFailed(errors);
        }

        validation.TryGetValue(out string validatedEmail);
        NewsletterLog.SubscribeStarted(logger);

        return await subscriberStore.ExistsAsync(validatedEmail, cancellationToken).MatchAsync(
            async alreadySubscribed =>
            {
                if (alreadySubscribed)
                {
                    NewsletterLog.AlreadySubscribed(logger);

                    return NewsletterSubscriptionOutcomeFactory.AlreadySubscribed();
                }

                return await SubscribeNewAsync(validatedEmail, cancellationToken);
            },
            error =>
            {
                NewsletterLog.ExistsCheckFailed(logger, error);

                return Task.FromResult(NewsletterSubscriptionOutcomeFactory.SubscribeFailed(error));
            });
    }

    private Task<NewsletterSubscriptionOutcome> SubscribeNewAsync(string email, CancellationToken cancellationToken)
    {
        var subscriber = NewsletterSubscriberFactory.Create(email, timeProvider.GetUtcNow());

        return subscriberStore.AddAsync(subscriber, cancellationToken).MatchAsync(
            async _ =>
            {
                NewsletterLog.Subscribed(logger);

                var emailResult = await emailSender.SendAsync(subscriber, cancellationToken);
                emailResult.TapError(error => NewsletterLog.ConfirmationEmailFailed(logger, error));

                return NewsletterSubscriptionOutcomeFactory.Succeeded();
            },
            error =>
            {
                NewsletterLog.SubscribeFailed(logger, error);

                return Task.FromResult(NewsletterSubscriptionOutcomeFactory.SubscribeFailed(error));
            });
    }
}
