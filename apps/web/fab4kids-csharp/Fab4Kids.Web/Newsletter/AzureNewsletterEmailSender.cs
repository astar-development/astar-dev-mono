using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;

namespace Fab4Kids.Web.Newsletter;

/// <inheritdoc cref="INewsletterEmailSender"/>
public sealed class AzureNewsletterEmailSender(IOptions<NewsletterOptions> options, ILogger<AzureNewsletterEmailSender> logger, EmailClient? emailClient = null) : INewsletterEmailSender
{
    public Task<Result<UnitFp, string>> SendAsync(NewsletterSubscriber subscriber, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (emailClient is null || string.IsNullOrWhiteSpace(settings.FromAddress))
        {
            LogMessage.Error(logger, "Newsletter confirmation email is not configured (missing connection string or from address).");

            return Task.FromResult<Result<UnitFp, string>>("Something went wrong sending your confirmation email.");
        }

        return Try.RunAsync(async () =>
        {
            const string textBody = "Thanks for subscribing to fab4kids updates!\n\nWe will let you know when new curriculum-aligned resources are added.";
            const string htmlBody = "<p>Thanks for subscribing to fab4kids updates!</p><p>We will let you know when new curriculum-aligned resources are added.</p>";

            var content = new EmailContent("Welcome to fab4kids updates!") { PlainText = textBody, Html = htmlBody };
            var emailMessage = new EmailMessage(settings.FromAddress, subscriber.Email, content);

            await emailClient.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);

            return UnitFp.Instance;
        }, cancellationToken).ToResultAsync(ex =>
        {
            LogMessage.LogException(logger, nameof(AzureNewsletterEmailSender), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

            return "Something went wrong sending your confirmation email.";
        });
    }
}
