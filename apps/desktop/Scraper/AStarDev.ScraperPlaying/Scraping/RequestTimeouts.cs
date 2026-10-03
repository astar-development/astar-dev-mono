namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Tells a request that timed out apart from a scrape that was cancelled.</summary>
public static class RequestTimeouts
{
    /// <summary>Runs the request. <see cref="HttpClient"/> reports a timeout as a <see cref="TaskCanceledException"/> although nothing cancelled the scrape, and <c>Try</c> rethrows every <see cref="OperationCanceledException"/>, so a timeout would abort the whole scrape. It is turned into a <see cref="TimeoutException"/> instead, which fails only the one wallpaper or page.</summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="request">The request to run.</param>
    /// <param name="cancellationToken">The scrape's token: a cancellation it requested is left to propagate.</param>
    public static async Task<T> RunAsync<T>(Func<Task<T>> request, CancellationToken cancellationToken)
    {
        try
        {
            return await request();
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The request timed out.", exception);
        }
    }
}
