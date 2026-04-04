using AStar.Dev.Logging.Extensions;
using AStar.Dev.Sync.Engine.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Sync.Engine.Features.Resilience;

/// <summary>
///     Retries an async operation with exponential back-off: 2^attempt seconds, max 5 retries, capped at 60 s (EH-01).
///     Respects <see cref="CancellationToken"/> so shutdown is honoured during waits.
/// </summary>
public sealed class ExponentialBackoffPolicy(ILogger<ExponentialBackoffPolicy> logger)
{
    private const int MaxRetries = 5;
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    ///     Executes <paramref name="operation"/> with exponential back-off on failure.
    ///     Returns when the operation succeeds or all retries are exhausted (in which case the last exception is re-thrown).
    /// </summary>
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await operation(ct).ConfigureAwait(false);

                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                var delay = CalculateDelay(attempt);
                SyncEngineLogMessage.RetryingAfterDelay(logger, attempt + 1, MaxRetries, delay.TotalSeconds, ex.Message);

                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    ///     Executes <paramref name="operation"/> with exponential back-off on failure.
    ///     Returns the result when the operation succeeds or all retries are exhausted.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                return await operation(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                var delay = CalculateDelay(attempt);
                SyncEngineLogMessage.RetryingAfterDelay(logger, attempt + 1, MaxRetries, delay.TotalSeconds, ex.Message);

                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }

        return await operation(ct).ConfigureAwait(false);
    }

    internal static TimeSpan CalculateDelay(int attempt) => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), MaxDelay.TotalSeconds));
}
