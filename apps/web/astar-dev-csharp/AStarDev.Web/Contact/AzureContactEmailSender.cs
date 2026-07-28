using System.Text.Encodings.Web;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;

namespace AStarDev.Web.Contact;

/// <inheritdoc cref="IContactEmailSender"/>
public sealed class AzureContactEmailSender(IOptions<ContactFormOptions> options, ILogger<AzureContactEmailSender> logger, EmailClient? emailClient = null) : IContactEmailSender
{
    public Task<Result<UnitFp, string>> SendAsync(ContactMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (emailClient is null || string.IsNullOrWhiteSpace(settings.FromAddress) || string.IsNullOrWhiteSpace(settings.ToAddress))
        {
            LogMessage.Error(logger, "Contact form email is not configured (missing connection string, from address, or to address).");

            return Task.FromResult<Result<UnitFp, string>>("Something went wrong. Please try again later.");
        }

        return Try.RunAsync(async () =>
        {
            await SendOwnerNotificationAsync(emailClient, message, settings.FromAddress, settings.ToAddress, cancellationToken);

            if (message.SendCopy)
                await SendCopyToSenderAsync(emailClient, message, settings.FromAddress, cancellationToken);

            return UnitFp.Instance;
        }).ToResultAsync(ex =>
        {
            LogMessage.LogException(logger, nameof(AzureContactEmailSender), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

            return $"Something went wrong. Please email {settings.ToAddress} directly.";
        });
    }

    private static Task<EmailSendOperation> SendOwnerNotificationAsync(EmailClient client, ContactMessage message, string fromAddress, string toAddress, CancellationToken cancellationToken)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var textBody = $"Name: {message.Name}\nEmail: {message.Email}\nTimestamp: {timestamp:O}\n\nMessage:\n{message.Message}";
        var htmlBody =
            $"<p><strong>Name:</strong> {HtmlEncoder.Default.Encode(message.Name)}</p>" +
            $"<p><strong>Email:</strong> {HtmlEncoder.Default.Encode(message.Email)}</p>" +
            $"<p><strong>Timestamp:</strong> {timestamp:O}</p>" +
            "<hr />" +
            "<p><strong>Message:</strong></p>" +
            $"<p>{HtmlEncoder.Default.Encode(message.Message).Replace("\n", "<br />")}</p>";

        var content = new EmailContent($"[AStar.Dev Contact] Message from {message.Name}") { PlainText = textBody, Html = htmlBody };
        var emailMessage = new EmailMessage(fromAddress, toAddress, content);

        return client.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);
    }

    private static Task<EmailSendOperation> SendCopyToSenderAsync(EmailClient client, ContactMessage message, string fromAddress, CancellationToken cancellationToken)
    {
        var textBody = $"Hi {message.Name},\n\nThis is a copy of the message you sent to AStar Development.\nWe will be in touch as soon as possible.\n\n---\n\n{message.Message}";
        var htmlBody =
            $"<p>Hi {HtmlEncoder.Default.Encode(message.Name)},</p>" +
            "<p>This is a copy of the message you sent to AStar Development.</p>" +
            "<p>We will be in touch as soon as possible.</p>" +
            "<hr />" +
            $"<p>{HtmlEncoder.Default.Encode(message.Message).Replace("\n", "<br />")}</p>";

        var content = new EmailContent("Copy of your message to AStar Development") { PlainText = textBody, Html = htmlBody };
        var emailMessage = new EmailMessage(fromAddress, message.Email, content);

        return client.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);
    }
}
