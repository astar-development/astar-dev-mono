namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Http;

/// <summary>
/// Shared HTTP retry constants and helpers for exponential backoff with jitter
/// and Retry-After header parsing. Used by both download and upload pipelines.
/// </summary>
public static class HttpRetryPolicy
{
    /// <summary>Maximum number of retry attempts before the operation is considered failed.</summary>
    public const int MaxRetries = 5;

    /// <summary>Base delay in seconds for the first exponential backoff step.</summary>
    public const double BaseDelaySeconds = 2.0;

    /// <summary>Upper bound in seconds for any single backoff delay before jitter is applied.</summary>
    public const double MaxDelaySeconds = 120.0;

    /// <summary>
    /// Computes the delay to wait before a retry attempt, honouring the
    /// <c>Retry-After</c> response header when present. Falls back to
    /// <see cref="GetBackoffDelay"/> when no header is available.
    /// </summary>
    /// <param name="response">The HTTP response that triggered the retry (typically 429).</param>
    /// <param name="attempt">The 1-based attempt number that just failed.</param>
    /// <returns>The <see cref="TimeSpan"/> to delay before the next attempt.</returns>
    public static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
            return delta + TimeSpan.FromSeconds(1);

        if (response.Headers.RetryAfter?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero)
                return wait + TimeSpan.FromSeconds(1);
        }

        return GetBackoffDelay(attempt);
    }

    /// <summary>
    /// Computes an exponential backoff delay with up to 20% random jitter for the given attempt number.
    /// The base delay is capped at <see cref="MaxDelaySeconds"/> before jitter is added.
    /// </summary>
    /// <param name="attempt">The 1-based attempt number that just failed.</param>
    /// <returns>A <see cref="TimeSpan"/> in the range <c>[base, base * 1.2)</c> where <c>base = min(2^(attempt-1) * 2, 120)</c>.</returns>
    public static TimeSpan GetBackoffDelay(int attempt)
    {
        double seconds = Math.Min(BaseDelaySeconds * Math.Pow(2, attempt - 1), MaxDelaySeconds);
        double jitter = seconds * 0.2 * Random.Shared.NextDouble();

        return TimeSpan.FromSeconds(seconds + jitter);
    }
}
