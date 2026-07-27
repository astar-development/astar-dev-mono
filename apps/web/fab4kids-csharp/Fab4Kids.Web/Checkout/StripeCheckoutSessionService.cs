using System.Globalization;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Logging.Extensions;
using Fab4Kids.Web.Cart;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace Fab4Kids.Web.Checkout;

/// <inheritdoc cref="ICheckoutSessionService"/>
public sealed class StripeCheckoutSessionService(IOptions<CheckoutOptions> options, ILogger<StripeCheckoutSessionService> logger, SessionService? sessionService = null) : ICheckoutSessionService
{
    public async Task<CheckoutSessionOutcome> CreateSessionAsync(IReadOnlyList<CartItem> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            LogMessage.Warning(logger, "checkout/invalid-body", "empty-cart");

            return CheckoutSessionOutcomeFactory.CartEmpty();
        }

        var settings = options.Value;
        if (sessionService is null || string.IsNullOrWhiteSpace(settings.SiteUrl))
        {
            LogMessage.Error(logger, "Stripe checkout is not configured (missing secret key or site URL).");

            return CheckoutSessionOutcomeFactory.Failed("Checkout is currently unavailable.");
        }

        var itemCount = items.Count.ToString(CultureInfo.InvariantCulture);
        LogMessage.Information(logger, "checkout/session-start", itemCount);

        var sessionOptions = BuildSessionOptions(items, settings.SiteUrl);

        return await Try.RunAsync(() => sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken)).ToResultAsync(ex =>
        {
            LogMessage.LogException(logger, nameof(StripeCheckoutSessionService), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

            return "Unable to create checkout session.";
        }).MatchAsync(
            session =>
            {
                LogMessage.Information(logger, "checkout/session-created", session.Id);

                return CheckoutSessionOutcomeFactory.Created(session.Url);
            },
            error => CheckoutSessionOutcomeFactory.Failed(error));
    }

    public async Task<Option<string>> GetCustomerEmailAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (sessionService is null || string.IsNullOrWhiteSpace(sessionId))
            return Option.None<string>();

        var result = await Try.RunAsync(() => sessionService.GetAsync(sessionId, cancellationToken: cancellationToken)).ToResultAsync(ex =>
        {
            LogMessage.LogException(logger, nameof(StripeCheckoutSessionService), ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);

            return ex.Message;
        });

        return result.Match(
            session => string.IsNullOrWhiteSpace(session.CustomerDetails?.Email) ? Option.None<string>() : Option.Some(session.CustomerDetails.Email),
            _ => Option.None<string>());
    }

    private static SessionCreateOptions BuildSessionOptions(IReadOnlyList<CartItem> items, string siteUrl) => new()
    {
        Mode = "payment",
        LineItems = [.. items.Select(item => new SessionLineItemOptions
        {
            Quantity = item.Quantity,
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = "gbp",
                UnitAmount = (long)(item.Price * 100),
                ProductData = new SessionLineItemPriceDataProductDataOptions { Name = item.Name }
            }
        })],
        AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
        SuccessUrl = $"{siteUrl}/checkout/success?session_id={{CHECKOUT_SESSION_ID}}",
        CancelUrl = siteUrl,
        Metadata = new Dictionary<string, string> { ["source"] = "fab4kids" }
    };
}
