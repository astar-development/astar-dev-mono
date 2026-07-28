using AStar.Dev.FunctionalParadigm;

namespace Fab4Kids.Web.Fulfilment;

/// <summary>Records Stripe checkout session IDs that have already been fulfilled, to guard against duplicate webhook deliveries.</summary>
public interface IIdempotencyStore
{
    /// <summary>Atomically records <paramref name="sessionId"/> as processed. Returns <c>true</c> when newly marked, <c>false</c> when it was already processed.</summary>
    Task<Result<bool, string>> TryMarkProcessedAsync(string sessionId, CancellationToken cancellationToken);
}
