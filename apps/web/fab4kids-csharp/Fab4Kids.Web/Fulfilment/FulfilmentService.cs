using System.Globalization;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Stripe;
using Stripe.Checkout;

namespace Fab4Kids.Web.Fulfilment;

/// <inheritdoc cref="IFulfilmentService"/>
public sealed class FulfilmentService(
    ILogger<FulfilmentService> logger,
    IIdempotencyStore idempotencyStore,
    IPdfDeliveryLinkGenerator linkGenerator,
    IDeliveryEmailSender emailSender,
    SessionService? sessionService = null) : IFulfilmentService
{
    private static readonly TimeSpan DeliveryLinkTtl = TimeSpan.FromMinutes(15);

    public async Task<FulfilmentOutcome> ProcessCheckoutCompletedAsync(string sessionId, CancellationToken cancellationToken)
    {
        var marked = await idempotencyStore.TryMarkProcessedAsync(sessionId, cancellationToken);
        var isDuplicate = marked.Match(newlyMarked => !newlyMarked, _ => false);
        if (isDuplicate)
        {
            LogMessage.Information(logger, "webhook/duplicate-event", sessionId);

            return FulfilmentOutcomeFactory.Duplicate();
        }

        if (sessionService is null)
        {
            LogMessage.Error(logger, "Stripe is not configured (missing secret key).");

            return FulfilmentOutcomeFactory.Failed("Checkout is currently unavailable.");
        }

        LogMessage.Information(logger, "webhook/delivery-start", sessionId);

        return await Try.RunAsync(() => sessionService.GetAsync(sessionId, ExpandedLineItemsOptions(), cancellationToken: cancellationToken))
            .ToResultAsync(ex =>
            {
                LogMessage.LogException(logger, nameof(FulfilmentService), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

                return "Unable to retrieve checkout session.";
            })
            .MatchAsync(
                session => DeliverAsync(session, cancellationToken),
                error =>
                {
                    LogMessage.Warning(logger, "webhook/delivery-failed", error);

                    return Task.FromResult<FulfilmentOutcome>(FulfilmentOutcomeFactory.Failed(error));
                });
    }

    public async Task<ResendOutcome> ResendAsync(string orderReference, string email, CancellationToken cancellationToken)
    {
        if (sessionService is null)
        {
            LogMessage.Error(logger, "Stripe is not configured (missing secret key).");

            return ResendOutcomeFactory.Failed("Unable to resend your download links.");
        }

        LogMessage.Information(logger, "resend/start", orderReference);

        return await Try.RunAsync(() => sessionService.GetAsync(orderReference, ExpandedLineItemsOptions(), cancellationToken: cancellationToken))
            .ToResultAsync(ex =>
            {
                LogMessage.LogException(logger, nameof(FulfilmentService), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

                return "Unable to resend your download links.";
            })
            .MatchAsync(
                session => CompleteResendAsync(session, orderReference, email, cancellationToken),
                error =>
                {
                    LogMessage.Warning(logger, "resend/failed", error);

                    return Task.FromResult<ResendOutcome>(ResendOutcomeFactory.Failed(error));
                });
    }

    private async Task<FulfilmentOutcome> DeliverAsync(Session session, CancellationToken cancellationToken)
    {
        var customerEmail = session.CustomerDetails?.Email;
        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            LogMessage.Warning(logger, "webhook/no-customer-email", session.Id);

            return FulfilmentOutcomeFactory.NoCustomerEmail();
        }

        var links = await BuildDeliveryLinksAsync(session.LineItems?.Data ?? [], cancellationToken);

        return await emailSender.SendAsync(customerEmail, session.Id, links, cancellationToken).MatchAsync(
            _ =>
            {
                var linkCount = links.Count.ToString(CultureInfo.InvariantCulture);
                LogMessage.Information(logger, "webhook/delivery-sent", session.Id, linkCount);

                return FulfilmentOutcomeFactory.Delivered(links.Count);
            },
            error =>
            {
                LogMessage.Warning(logger, "webhook/delivery-failed", error);

                return FulfilmentOutcomeFactory.Failed(error);
            });
    }

    private async Task<ResendOutcome> CompleteResendAsync(Session session, string orderReference, string email, CancellationToken cancellationToken)
    {
        var customerEmail = session.CustomerDetails?.Email;
        if (string.IsNullOrWhiteSpace(customerEmail) || !string.Equals(customerEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            LogMessage.Warning(logger, "resend/email-mismatch", orderReference);

            return ResendOutcomeFactory.EmailMismatch();
        }

        var links = await BuildDeliveryLinksAsync(session.LineItems?.Data ?? [], cancellationToken);

        return await emailSender.SendAsync(customerEmail, orderReference, links, cancellationToken).MatchAsync(
            _ =>
            {
                var linkCount = links.Count.ToString(CultureInfo.InvariantCulture);
                LogMessage.Information(logger, "resend/sent", orderReference, linkCount);

                return ResendOutcomeFactory.Sent(links.Count);
            },
            error =>
            {
                LogMessage.Warning(logger, "resend/failed", error);

                return ResendOutcomeFactory.Failed(error);
            });
    }

    private async Task<IReadOnlyList<DeliveryLink>> BuildDeliveryLinksAsync(IEnumerable<LineItem> lineItems, CancellationToken cancellationToken)
    {
        var links = new List<DeliveryLink>();
        var expiresAt = DateTimeOffset.UtcNow.Add(DeliveryLinkTtl);

        foreach (var lineItem in lineItems)
        {
            var blobPath = lineItem.Price?.Product?.Metadata?.GetValueOrDefault("blobPath");
            if (string.IsNullOrWhiteSpace(blobPath))
                continue;

            var urlResult = await linkGenerator.GenerateSignedUrlAsync(blobPath, cancellationToken);
            var url = urlResult.Match(value => value, _ => string.Empty);
            if (string.IsNullOrWhiteSpace(url))
                continue;

            links.Add(DeliveryLinkFactory.Create(lineItem.Price?.Product?.Name, url, expiresAt));
        }

        return links;
    }

    private static SessionGetOptions ExpandedLineItemsOptions() => new() { Expand = ["line_items", "line_items.data.price.product"] };
}
