using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;

namespace Fab4Kids.Web.Newsletter;

/// <inheritdoc cref="INewsletterSubscriptionService"/>
public sealed class NewsletterSubscriptionService(INewsletterSubscriberStore subscriberStore, INewsletterEmailSender emailSender, TimeProvider timeProvider, ILogger<NewsletterSubscriptionService> logger) : INewsletterSubscriptionService
{
    public async Task<NewsletterSubscriptionOutcome> SubscribeAsync(string email, bool consent, CancellationToken cancellationToken)
    {
        if (!consent)
        {
            LogMessage.Warning(logger, "newsletter/no-consent", email);

            return NewsletterSubscriptionOutcomeFactory.NoConsent();
        }

        var validation = NewsletterValidator.Validate(email);
        if (validation.TryGetErrors(out var errors))
        {
            LogMessage.Warning(logger, "newsletter/invalid-email", email);

            return NewsletterSubscriptionOutcomeFactory.ValidationFailed(errors);
        }

        validation.TryGetValue(out string validatedEmail);
        LogMessage.Information(logger, "newsletter/subscribe-start", validatedEmail);

        return await subscriberStore.ExistsAsync(validatedEmail, cancellationToken).MatchAsync(
            async alreadySubscribed =>
            {
                if (alreadySubscribed)
                {
                    LogMessage.Information(logger, "newsletter/already-subscribed", validatedEmail);

                    return NewsletterSubscriptionOutcomeFactory.AlreadySubscribed();
                }

                return await SubscribeNewAsync(validatedEmail, cancellationToken);
            },
            error =>
            {
                LogMessage.Error(logger, $"newsletter/exists-check-failed: {error}");

                return Task.FromResult(NewsletterSubscriptionOutcomeFactory.SubscribeFailed(error));
            });
    }

    private Task<NewsletterSubscriptionOutcome> SubscribeNewAsync(string email, CancellationToken cancellationToken)
    {
        var subscriber = NewsletterSubscriberFactory.Create(email, timeProvider.GetUtcNow());

        return subscriberStore.AddAsync(subscriber, cancellationToken).MatchAsync(
            async _ =>
            {
                LogMessage.Information(logger, "newsletter/subscribed", email);

                var emailResult = await emailSender.SendAsync(subscriber, cancellationToken);
                emailResult.TapError(error => LogMessage.Error(logger, $"newsletter/confirmation-email-failed: {error}"));

                return NewsletterSubscriptionOutcomeFactory.Succeeded();
            },
            error =>
            {
                LogMessage.Error(logger, $"newsletter/subscribe-failed: {error}");

                return Task.FromResult(NewsletterSubscriptionOutcomeFactory.SubscribeFailed(error));
            });
    }
}
