using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Waits on <see cref="IAccountThrottleGate"/> before each account request and reports 429 / 503 (with Retry-After) and successes after it. It does not retry; the caller decides.</summary>
/// <param name="gate">The per-account throttle gate.</param>
/// <param name="timeProvider">Converts a Retry-After date into a delay.</param>
internal sealed class AccountThrottleHandler(IAccountThrottleGate gate, TimeProvider timeProvider) : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw new NotImplementedException($"{gate}{timeProvider}{request}{cancellationToken}");
}
