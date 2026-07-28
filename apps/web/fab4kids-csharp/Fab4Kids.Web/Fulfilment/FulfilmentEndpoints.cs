using AStar.Dev.Logging.Extensions;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Fab4Kids.Web.Fulfilment;

public static class FulfilmentEndpoints
{
    public static void MapFulfilmentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/webhooks/stripe", HandleWebhookAsync);
        app.MapPost("/api/resend-links", HandleResendLinksAsync);
    }

    private static async Task<IResult> HandleWebhookAsync(HttpRequest request, IFulfilmentService fulfilmentService, IOptions<FulfilmentOptions> options, ILogger<FulfilmentService> logger, CancellationToken cancellationToken)
    {
        var signature = request.Headers["Stripe-Signature"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(signature))
        {
            LogMessage.Warning(logger, "webhook/missing-signature", "no Stripe-Signature header");

            return Results.BadRequest(new { error = "Missing Stripe-Signature header" });
        }

        using var reader = new StreamReader(request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var webhookSecret = options.Value.WebhookSecret;

        Event stripeEvent;
        try
        {
            if (string.IsNullOrWhiteSpace(webhookSecret))
                throw new StripeException("Webhook secret is not configured.");

            stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);
        }
        catch (Exception ex)
        {
            LogMessage.LogException(logger, "webhook/signature-verification-failed", ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

            return Results.BadRequest(new { error = "Invalid signature" });
        }

        if (stripeEvent.Type == "checkout.session.completed" && stripeEvent.Data.Object is Session session)
            await fulfilmentService.ProcessCheckoutCompletedAsync(session.Id, cancellationToken);

        return Results.Ok();
    }

    private static async Task<IResult> HandleResendLinksAsync(ResendLinksRequest request, IFulfilmentService fulfilmentService, ILogger<FulfilmentService> logger, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderReference) || string.IsNullOrWhiteSpace(request.Email))
        {
            LogMessage.Warning(logger, "resend/invalid-body", "missing-fields");

            return Results.BadRequest(new { error = "Invalid request" });
        }

        var outcome = await fulfilmentService.ResendAsync(request.OrderReference, request.Email, cancellationToken);

        return outcome switch
        {
            ResendSent => Results.Ok(new { success = true }),
            ResendEmailMismatch => Results.BadRequest(new { error = "Invalid order reference or email" }),
            ResendFailed failed => Results.Problem(failed.Message, statusCode: StatusCodes.Status500InternalServerError),
            _ => Results.Problem("Unexpected resend outcome.", statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
