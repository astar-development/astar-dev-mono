namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>How long the scraper waits on the network before it gives up on a request.</summary>
/// <param name="Request">The time one request is allowed to take, from sending it until the response headers arrive.</param>
/// <param name="ImageBody">The time the whole body of one image download is allowed to take once its headers have arrived. <see cref="HttpClient.Timeout"/> does not cover it, because the download reads with <see cref="HttpCompletionOption.ResponseHeadersRead"/>.</param>
public sealed record ScrapeTimeouts(TimeSpan Request, TimeSpan ImageBody)
{
    /// <summary>The production timeouts: a minute for a request and five minutes for an image body.</summary>
    public static ScrapeTimeouts Default { get; } = new(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));

    /// <summary>
    /// The timeout of the Wallhaven <see cref="HttpClient"/>. It covers the whole call through the rate-limiting handler, so the waits before each
    /// <c>429 Too Many Requests</c> retry count against it. It therefore allows <see cref="Request"/> plus the longest default wait for every retry;
    /// a <c>Retry-After</c> longer than that is reported as a timeout rather than waited out.
    /// </summary>
    public TimeSpan Client => Request + (WallhavenRateLimitingHandler.DefaultRetryDelay * WallhavenRateLimitingHandler.MaximumRetries);
}
