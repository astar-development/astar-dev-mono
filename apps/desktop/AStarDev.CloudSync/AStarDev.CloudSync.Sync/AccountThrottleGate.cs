using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>An in-process <see cref="IAccountThrottleGate"/> timed by <see cref="TimeProvider"/>.</summary>
/// <param name="timeProvider">The clock and timer source.</param>
internal sealed class AccountThrottleGate(TimeProvider timeProvider) : IAccountThrottleGate
{
    /// <inheritdoc />
    public Task WaitAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{timeProvider}{accountId}{cancellationToken}");

    /// <inheritdoc />
    public void ReportThrottled(AccountId accountId, Option<TimeSpan> retryAfter) => throw new NotImplementedException($"{accountId}{retryAfter}");

    /// <inheritdoc />
    public void ReportSucceeded(AccountId accountId) => throw new NotImplementedException($"{accountId}");
}
