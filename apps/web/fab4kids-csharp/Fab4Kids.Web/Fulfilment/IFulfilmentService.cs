namespace Fab4Kids.Web.Fulfilment;

/// <summary>Orchestrates PDF delivery after a successful Stripe checkout, and resends links on request.</summary>
public interface IFulfilmentService
{
    Task<FulfilmentOutcome> ProcessCheckoutCompletedAsync(string sessionId, CancellationToken cancellationToken);

    Task<ResendOutcome> ResendAsync(string orderReference, string email, CancellationToken cancellationToken);
}
