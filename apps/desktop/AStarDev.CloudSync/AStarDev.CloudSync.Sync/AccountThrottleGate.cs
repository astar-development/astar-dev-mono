using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>An in-process <see cref="IAccountThrottleGate"/> timed by <see cref="TimeProvider"/>.</summary>
/// <param name="timeProvider">The clock and timer source.</param>
internal sealed class AccountThrottleGate(TimeProvider timeProvider) : IAccountThrottleGate
{
    private const int MaximumDoublings = 16;

    private readonly Lock gate = new();
    private readonly Dictionary<AccountId, (DateTimeOffset PausedUntil, int ConsecutiveThrottles)> accounts = [];

    /// <inheritdoc />
    public async Task WaitAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var remaining = Remaining(accountId);
        while (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, timeProvider, cancellationToken).ConfigureAwait(false);
            remaining = Remaining(accountId);
        }
    }

    /// <inheritdoc />
    public void ReportThrottled(AccountId accountId, Option<TimeSpan> retryAfter)
    {
        lock (gate)
        {
            var (pausedUntil, consecutive) = accounts.GetValueOrDefault(accountId, (DateTimeOffset.MinValue, 0));
            var pause = retryAfter.Match(Clamp, () => Backoff(consecutive));
            var candidate = timeProvider.GetUtcNow() + pause;
            accounts[accountId] = (candidate > pausedUntil ? candidate : pausedUntil, consecutive + 1);
        }
    }

    /// <inheritdoc />
    public void ReportSucceeded(AccountId accountId)
    {
        lock (gate)
        {
            if (accounts.TryGetValue(accountId, out var state))
                accounts[accountId] = (state.PausedUntil, 0);
        }
    }

    private static TimeSpan Clamp(TimeSpan retryAfter)
    {
        if (retryAfter <= TimeSpan.Zero)
            return TimeSpan.Zero;

        return retryAfter > ThrottlePolicy.MaximumRetryAfter ? ThrottlePolicy.MaximumRetryAfter : retryAfter;
    }

    private static TimeSpan Backoff(int consecutiveThrottles) => TimeSpan.FromTicks(Math.Min(ThrottlePolicy.MaximumBackoff.Ticks, ThrottlePolicy.InitialBackoff.Ticks << Math.Min(consecutiveThrottles, MaximumDoublings)));

    private TimeSpan Remaining(AccountId accountId)
    {
        lock (gate)
            return accounts.TryGetValue(accountId, out var state) ? state.PausedUntil - timeProvider.GetUtcNow() : TimeSpan.Zero;
    }
}
