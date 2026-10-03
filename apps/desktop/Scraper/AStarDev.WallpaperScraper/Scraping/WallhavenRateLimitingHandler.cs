using System.Net;
using System.Threading.RateLimiting;

namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>
/// Keeps outbound Wallhaven API calls within the API's rate limit. Every API request first acquires a permit from the shared
/// <see cref="RateLimiter"/>; a <c>429 Too Many Requests</c> response is retried after the delay in its <c>Retry-After</c> header.
/// Requests outside the API path (image downloads from the CDN) are not limited.
/// </summary>
/// <param name="limiter">The limiter shared by every request to the Wallhaven API.</param>
/// <param name="notifier">Told how long each back-off is before it starts, so the user can be shown it.</param>
/// <param name="timeProvider">The time source used to wait before retrying.</param>
public sealed class WallhavenRateLimitingHandler(RateLimiter limiter, IRateLimitBackoffNotifier notifier, TimeProvider timeProvider) : DelegatingHandler
{
    /// <summary>The number of times a request that received a 429 is retried before the 429 is returned to the caller.</summary>
    public const int MaximumRetries = 3;

    /// <summary>The wait applied to a 429 that has no usable <c>Retry-After</c> header.</summary>
    public static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromMinutes(1);

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!WallhavenApiRequests.IsApiRequest(request)) return await base.SendAsync(request, cancellationToken);

        var response = await SendWithPermitAsync(request, cancellationToken);
        for (var retry = 0; retry < MaximumRetries && response.StatusCode == HttpStatusCode.TooManyRequests; retry++)
        {
            var delay = RetryDelayFor(response);
            response.Dispose();
            notifier.NotifyBackingOff(delay);
            await Task.Delay(delay, timeProvider, cancellationToken);
            using var retryRequest = CloneRequest(request);
            response = await SendWithPermitAsync(retryRequest, cancellationToken);
        }

        return response;
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };
        foreach (var header in request.Headers)
        {
            _ = clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private async Task<HttpResponseMessage> SendWithPermitAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var lease = await limiter.AcquireAsync(1, cancellationToken);

        return await base.SendAsync(request, cancellationToken);
    }

    private TimeSpan RetryDelayFor(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta) return delta;

        return retryAfter?.Date is { } date ? Max(date - timeProvider.GetUtcNow(), TimeSpan.Zero) : DefaultRetryDelay;
    }

    private static TimeSpan Max(TimeSpan first, TimeSpan second) => first > second ? first : second;
}
