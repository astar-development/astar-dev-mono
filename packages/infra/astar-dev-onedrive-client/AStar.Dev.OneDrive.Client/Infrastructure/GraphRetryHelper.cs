using AStar.Dev.Logging.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;

namespace AStar.Dev.OneDrive.Client.Infrastructure;

internal static class GraphRetryHelper
{
    internal const int MaxRetries = 3;
    private const int DefaultRetryAfterSeconds = 30;

    internal static async Task<T> CallWithRetryAsync<T>(Func<Task<T>> operation, ILogger logger, CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (ODataError oDataError) when (oDataError.ResponseStatusCode == 429)
            {
                if (attempt == MaxRetries - 1)
                    throw;

                var delay = GetRetryAfterDelay(oDataError);
                LogMessage.GraphApiThrottled(logger, attempt + 1, MaxRetries, delay.TotalSeconds);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("Exceeded maximum retries without a result.");
    }

    private static TimeSpan GetRetryAfterDelay(ODataError oDataError)
    {
        if (oDataError.ResponseHeaders?.TryGetValue("Retry-After", out var values) == true)
        {
            var headerValue = values?.FirstOrDefault();
            if (int.TryParse(headerValue, out var seconds))
                return TimeSpan.FromSeconds(seconds);
        }

        return TimeSpan.FromSeconds(DefaultRetryAfterSeconds);
    }
}
