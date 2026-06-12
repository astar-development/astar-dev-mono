using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Http;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Jobs;

public sealed class GivenAnHttpDownloaderBackoff
{
    [Fact]
    public void when_attempt_is_1_then_backoff_delay_is_between_2_and_2_point_4_seconds()
    {
        var result = HttpRetryPolicy.GetBackoffDelay(1);

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(2.0);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(2.4);
    }

    [Fact]
    public void when_attempt_is_2_then_backoff_delay_is_between_4_and_4_point_8_seconds()
    {
        var result2 = HttpRetryPolicy.GetBackoffDelay(2);

        result2.TotalSeconds.ShouldBeGreaterThanOrEqualTo(4.0);
        result2.TotalSeconds.ShouldBeLessThanOrEqualTo(4.8);
    }

    [Fact]
    public void when_attempts_increase_then_delays_increase_exponentially()
    {
        var delays = new List<TimeSpan>();

        for (int i = 1; i <= 5; i++)
        {
            delays.Add(HttpRetryPolicy.GetBackoffDelay(i));
        }

        delays[0].TotalSeconds.ShouldBeLessThan(delays[1].TotalSeconds);
        delays[1].TotalSeconds.ShouldBeLessThan(delays[2].TotalSeconds);
        delays[2].TotalSeconds.ShouldBeLessThan(delays[3].TotalSeconds);
        delays[3].TotalSeconds.ShouldBeLessThan(delays[4].TotalSeconds);
    }

    [Fact]
    public void when_attempt_is_10_then_delay_is_capped_at_144_seconds()
    {
        var result = HttpRetryPolicy.GetBackoffDelay(10);

        result.TotalSeconds.ShouldBeLessThanOrEqualTo(144);
    }

    [Fact]
    public void when_called_multiple_times_then_delays_have_variability()
    {
        var delays = new List<TimeSpan>();
        for (int i = 0; i < 10; i++)
        {
            delays.Add(HttpRetryPolicy.GetBackoffDelay(1));
        }

        int uniqueValues = delays.DistinctBy(d => d.TotalMilliseconds).Count();
        uniqueValues.ShouldBeGreaterThan(1);
    }

    [Theory]
    [InlineData(1, 2.0, 2.4)]
    [InlineData(2, 4.0, 4.8)]
    [InlineData(3, 8.0, 9.6)]
    [InlineData(4, 16.0, 19.2)]
    [InlineData(5, 32.0, 38.4)]
    public void when_attempt_is_specified_then_delay_respects_ceiling_and_jitter(int attempt, double minSeconds, double maxSeconds)
    {
        var result = HttpRetryPolicy.GetBackoffDelay(attempt);

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(minSeconds);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(maxSeconds);
    }
}
