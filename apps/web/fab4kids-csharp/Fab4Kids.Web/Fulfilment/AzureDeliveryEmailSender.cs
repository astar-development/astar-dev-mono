using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;

namespace Fab4Kids.Web.Fulfilment;

/// <inheritdoc cref="IDeliveryEmailSender"/>
public sealed class AzureDeliveryEmailSender(IOptions<FulfilmentOptions> options, ILogger<AzureDeliveryEmailSender> logger, EmailClient? emailClient = null) : IDeliveryEmailSender
{
    public Task<Result<UnitFp, string>> SendAsync(string toAddress, string orderReference, IReadOnlyList<DeliveryLink> links, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (emailClient is null || string.IsNullOrWhiteSpace(settings.FromAddress))
        {
            LogMessage.Error(logger, "Delivery email is not configured (missing connection string or from address).");

            return Task.FromResult(Result.Failure<UnitFp, string>("Something went wrong sending your download links."));
        }

        return Try.RunAsync(async () =>
        {
            var content = new EmailContent("Your fab4kids order is ready to download") { PlainText = BuildText(orderReference, links), Html = BuildHtml(orderReference, links) };
            var emailMessage = new EmailMessage(settings.FromAddress, toAddress, content);

            await emailClient.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);

            return UnitFp.Instance;
        }, cancellationToken).ToResultAsync(ex =>
        {
            LogMessage.LogException(logger, nameof(AzureDeliveryEmailSender), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

            return "Something went wrong sending your download links.";
        });
    }

    private static string BuildHtml(string orderReference, IReadOnlyList<DeliveryLink> links)
    {
        var items = string.Join(string.Empty, links.Select(link => $"<li><strong>{link.ProductTitle}</strong><br><a href=\"{link.Url}\">Download</a> (expires {link.ExpiresAt:HH:mm})</li>"));

        return $"<h1>Your fab4kids order is ready!</h1><p>Order reference: <code>{orderReference}</code></p><p>Links expire 15 minutes after this email was sent.</p><ul>{items}</ul><p>Thank you for supporting fab4kids!</p>";
    }

    private static string BuildText(string orderReference, IReadOnlyList<DeliveryLink> links)
    {
        var items = string.Join('\n', links.Select(link => $"{link.ProductTitle}: {link.Url} (expires {link.ExpiresAt:O})"));

        return $"Your fab4kids order is ready!\n\nOrder: {orderReference}\n\n{items}\n\nThank you!";
    }
}
