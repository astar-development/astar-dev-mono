using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenScrapeTimeouts
{
    [Fact]
    public void when_the_client_timeout_is_worked_out_then_it_allows_one_request_plus_every_rate_limit_retry_wait()
    {
        var timeouts = new ScrapeTimeouts(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));

        timeouts.Client.ShouldBe(TimeSpan.FromSeconds(30) + (WallhavenRateLimitingHandler.DefaultRetryDelay * WallhavenRateLimitingHandler.MaximumRetries));
    }

    [Fact]
    public void when_the_default_timeouts_are_used_then_the_client_timeout_outlasts_a_request_that_is_retried_after_every_default_wait()
    {
        var retryWaits = WallhavenRateLimitingHandler.DefaultRetryDelay * WallhavenRateLimitingHandler.MaximumRetries;

        ScrapeTimeouts.Default.Client.ShouldBeGreaterThan(retryWaits);
    }
}
