using System.Net.Http.Headers;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Http;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Http;

public sealed class GivenAnHttpRetryPolicy
{
    [Fact]
    public void when_attempt_is_1_then_backoff_delay_is_near_base_delay()
    {
        var result = HttpRetryPolicy.GetBackoffDelay(1);

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(HttpRetryPolicy.BaseDelaySeconds);
        result.TotalSeconds.ShouldBeLessThan(HttpRetryPolicy.BaseDelaySeconds * 1.2 + 0.001);
    }

    [Fact]
    public void when_attempt_grows_then_backoff_delay_grows()
    {
        var delay1 = HttpRetryPolicy.GetBackoffDelay(1);
        var delay2 = HttpRetryPolicy.GetBackoffDelay(2);
        var delay3 = HttpRetryPolicy.GetBackoffDelay(3);

        delay1.TotalSeconds.ShouldBeLessThan(delay2.TotalSeconds);
        delay2.TotalSeconds.ShouldBeLessThan(delay3.TotalSeconds);
    }

    [Fact]
    public void when_attempt_is_very_large_then_delay_is_capped_at_max_plus_jitter()
    {
        var result = HttpRetryPolicy.GetBackoffDelay(100);

        result.TotalSeconds.ShouldBeLessThanOrEqualTo(HttpRetryPolicy.MaxDelaySeconds * 1.2 + 0.001);
    }

    [Theory]
    [InlineData(1, 2.0, 2.4)]
    [InlineData(2, 4.0, 4.8)]
    [InlineData(3, 8.0, 9.6)]
    [InlineData(4, 16.0, 19.2)]
    [InlineData(5, 32.0, 38.4)]
    public void when_attempt_is_specified_then_delay_is_within_jitter_band(int attempt, double minSeconds, double maxSeconds)
    {
        var result = HttpRetryPolicy.GetBackoffDelay(attempt);

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(minSeconds);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(maxSeconds);
    }

    [Fact]
    public void when_retry_after_delta_header_is_present_then_retry_delay_uses_delta_plus_one_second()
    {
        using var response = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        var result = HttpRetryPolicy.GetRetryDelay(response, 1);

        result.TotalSeconds.ShouldBe(31.0, tolerance: 0.01);
    }

    [Fact]
    public void when_retry_after_date_header_is_in_the_future_then_retry_delay_uses_remaining_time_plus_one_second()
    {
        using var response = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        var retryAt = DateTimeOffset.UtcNow.AddSeconds(20);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAt);

        var result = HttpRetryPolicy.GetRetryDelay(response, 1);

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(19.0);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(22.0);
    }

    [Fact]
    public void when_retry_after_date_header_is_in_the_past_then_retry_delay_falls_back_to_backoff()
    {
        using var response = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddSeconds(-5));

        var result = HttpRetryPolicy.GetRetryDelay(response, 1);

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(HttpRetryPolicy.BaseDelaySeconds);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(HttpRetryPolicy.BaseDelaySeconds * 1.2 + 0.001);
    }

    [Fact]
    public void when_retry_after_header_is_absent_then_retry_delay_falls_back_to_backoff()
    {
        using var response = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);

        var result = HttpRetryPolicy.GetRetryDelay(response, 1);

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(HttpRetryPolicy.BaseDelaySeconds);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(HttpRetryPolicy.BaseDelaySeconds * 1.2 + 0.001);
    }

    [Fact]
    public void when_retry_after_delta_is_zero_then_retry_delay_is_one_second()
    {
        using var response = new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);

        var result = HttpRetryPolicy.GetRetryDelay(response, 1);

        result.TotalSeconds.ShouldBe(1.0, tolerance: 0.01);
    }
}
