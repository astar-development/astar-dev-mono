using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Pauses every provider request of an account after the provider throttles it, so parallel transfers stop together.</summary>
public interface IAccountThrottleGate
{
    /// <summary>Waits until the account is not paused.</summary>
    /// <param name="accountId">The account about to send a request.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    Task WaitAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Records a throttled response (429 or 503). With <paramref name="retryAfter"/> the account pauses for that long, capped at <see cref="ThrottlePolicy.MaximumRetryAfter"/>; without it the pause backs off from <see cref="ThrottlePolicy.InitialBackoff"/>, doubling per consecutive throttle up to <see cref="ThrottlePolicy.MaximumBackoff"/>.</summary>
    /// <param name="accountId">The throttled account.</param>
    /// <param name="retryAfter">The provider's Retry-After, when it sent one.</param>
    void ReportThrottled(AccountId accountId, Option<TimeSpan> retryAfter);

    /// <summary>Records a successful response, which resets the back-off.</summary>
    /// <param name="accountId">The account.</param>
    void ReportSucceeded(AccountId accountId);
}
