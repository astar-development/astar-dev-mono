using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>An in-process <see cref="IAccountThrottleGate"/> timed by <see cref="TimeProvider"/>.</summary>
/// <param name="timeProvider">The clock and timer source.</param>
internal sealed class AccountThrottleGate(TimeProvider timeProvider) : IAccountThrottleGate, IAccountThrottleHints
{
    private const int MaximumDoublings = 16;

    private readonly Lock gate = new();
    private readonly Dictionary<AccountId, (DateTimeOffset PausedUntil, int ConsecutiveThrottles)> accounts = [];
    private readonly Dictionary<AccountId, Queue<DateTimeOffset>> throttles = [];

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
            var now = timeProvider.GetUtcNow();
            var (pausedUntil, consecutive) = accounts.GetValueOrDefault(accountId, (DateTimeOffset.MinValue, 0));
            if (now < pausedUntil)
            {
                accounts[accountId] = (Later(pausedUntil, now + retryAfter.Match(Clamp, () => TimeSpan.Zero)), consecutive);

                return;
            }

            accounts[accountId] = (now + retryAfter.Match(Clamp, () => Backoff(consecutive)), consecutive + 1);
            Recent(accountId, now).Enqueue(now);
        }
    }

    /// <inheritdoc />
    public void ReportSucceeded(AccountId accountId)
    {
        lock (gate)
        {
            if (accounts.TryGetValue(accountId, out var state) && timeProvider.GetUtcNow() >= state.PausedUntil)
                accounts[accountId] = (state.PausedUntil, 0);
        }
    }

    /// <inheritdoc />
    public ThrottleHint HintFor(AccountId accountId)
    {
        lock (gate)
            return Recent(accountId, timeProvider.GetUtcNow()).Count >= ThrottlePolicy.HintThrottleCount ? ThrottleHintFactory.CreateSuggestLowerConcurrency() : ThrottleHintFactory.CreateQuiet();
    }

    private static DateTimeOffset Later(DateTimeOffset first, DateTimeOffset second) => first > second ? first : second;

    private static TimeSpan Clamp(TimeSpan retryAfter)
    {
        if (retryAfter <= TimeSpan.Zero)
            return TimeSpan.Zero;

        return retryAfter > ThrottlePolicy.MaximumRetryAfter ? ThrottlePolicy.MaximumRetryAfter : retryAfter;
    }

    private static TimeSpan Backoff(int consecutiveThrottles) => TimeSpan.FromTicks(Math.Min(ThrottlePolicy.MaximumBackoff.Ticks, ThrottlePolicy.InitialBackoff.Ticks << Math.Min(consecutiveThrottles, MaximumDoublings)));

    private Queue<DateTimeOffset> Recent(AccountId accountId, DateTimeOffset now)
    {
        if (!throttles.TryGetValue(accountId, out var recent))
            throttles[accountId] = recent = new Queue<DateTimeOffset>();

        while (recent.TryPeek(out var oldest) && now - oldest >= ThrottlePolicy.HintWindow)
            _ = recent.Dequeue();

        return recent;
    }

    private TimeSpan Remaining(AccountId accountId)
    {
        lock (gate)
            return accounts.TryGetValue(accountId, out var state) ? state.PausedUntil - timeProvider.GetUtcNow() : TimeSpan.Zero;
    }
}
