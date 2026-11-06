using System.Security.Cryptography;
using System.Text;
using AStarDev.FunctionalParadigm;

namespace AStarDev.Web.Contact;

/// <inheritdoc cref="IContactSubmissionService"/>
public sealed class ContactSubmissionService(IContactRateLimiter rateLimiter, IContactEmailSender emailSender, ILogger<ContactSubmissionService> logger) : IContactSubmissionService
{
    public async Task<ContactSubmissionOutcome> SubmitAsync(string name, string email, string message, bool sendCopy, string website, string ipAddress, CancellationToken cancellationToken)
    {
        if (website.Length > 0)
        {
            ContactLog.HoneypotTriggered(logger, ClientHashOf(ipAddress));

            return ContactSubmissionOutcomeFactory.Succeeded();
        }

        if (!rateLimiter.TryAcquire(ipAddress))
        {
            ContactLog.RateLimitExceeded(logger, ClientHashOf(ipAddress));

            return ContactSubmissionOutcomeFactory.RateLimited();
        }

        var validation = ContactFormValidator.Validate(name, email, message, sendCopy);
        if (validation.TryGetErrors(out var errors))
        {
            string invalidProperties = string.Join(",", errors.Select(e => e.Property));
            ContactLog.ValidationFailed(logger, invalidProperties);

            return ContactSubmissionOutcomeFactory.ValidationFailed(errors);
        }

        validation.TryGetValue(out var contactMessage);
        ContactLog.SendStarted(logger);

        var sendResult = await emailSender.SendAsync(contactMessage, cancellationToken);

        return sendResult.Match(
            _ =>
            {
                ContactLog.SendSucceeded(logger);

                return ContactSubmissionOutcomeFactory.Succeeded();
            },
            ContactSubmissionOutcomeFactory.SendFailed);
    }

    // shortcut: unsalted SHA-256 of an IPv4 address can be brute-forced; switch to an HMAC with a configured secret if the hash must resist reversal.
    private static string ClientHashOf(string ipAddress) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress)))[..16];
}
