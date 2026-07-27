using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.Web.Contact;

/// <inheritdoc cref="IContactSubmissionService"/>
public sealed class ContactSubmissionService(IContactRateLimiter rateLimiter, IContactEmailSender emailSender, ILogger<ContactSubmissionService> logger) : IContactSubmissionService
{
    public async Task<ContactSubmissionOutcome> SubmitAsync(string name, string email, string message, bool sendCopy, string website, string ipAddress, CancellationToken cancellationToken)
    {
        if (website.Length > 0)
        {
            LogMessage.Warning(logger, "contact/honeypot-triggered", ipAddress);

            return ContactSubmissionOutcomeFactory.Succeeded();
        }

        if (!rateLimiter.TryAcquire(ipAddress))
        {
            LogMessage.Warning(logger, "contact/rate-limit-exceeded", ipAddress);

            return ContactSubmissionOutcomeFactory.RateLimited();
        }

        var validation = ContactFormValidator.Validate(name, email, message, sendCopy);
        if (validation.TryGetErrors(out var errors))
        {
            var invalidProperties = string.Join(",", errors.Select(e => e.Property));
            LogMessage.Debug(logger, "contact/validation-failed", invalidProperties);

            return ContactSubmissionOutcomeFactory.ValidationFailed(errors);
        }

        validation.TryGetValue(out var contactMessage);
        LogMessage.Information(logger, "contact/send-start", contactMessage.Email);

        var sendResult = await emailSender.SendAsync(contactMessage, cancellationToken);

        return sendResult.Match(
            _ =>
            {
                LogMessage.Information(logger, "contact/send-success", contactMessage.Email);

                return ContactSubmissionOutcomeFactory.Succeeded();
            },
            ContactSubmissionOutcomeFactory.SendFailed);
    }
}
