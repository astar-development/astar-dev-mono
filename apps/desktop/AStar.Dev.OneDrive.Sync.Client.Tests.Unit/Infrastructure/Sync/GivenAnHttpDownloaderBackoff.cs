using System.Reflection;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenAnHttpDownloaderBackoff
{
    [Fact]
    public void when_attempt_is_1_then_backoff_delay_is_between_2_and_2_point_4_seconds()
    {
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", BindingFlags.NonPublic | BindingFlags.Static);
        _ = method.ShouldNotBeNull();

        var result = (TimeSpan)method!.Invoke(null, [1])!;
        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(2.0);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(2.4);
    }

    [Fact]
    public void when_attempt_is_2_then_backoff_delay_is_between_4_and_4_point_8_seconds()
    {
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", BindingFlags.NonPublic | BindingFlags.Static);
        _ = method.ShouldNotBeNull();

        _ = (TimeSpan)method!.Invoke(null, [1])!;
        var result2 = (TimeSpan)method!.Invoke(null, [2])!;
        result2.TotalSeconds.ShouldBeGreaterThanOrEqualTo(4.0);
        result2.TotalSeconds.ShouldBeLessThanOrEqualTo(4.8);
    }

    [Fact]
    public void when_attempts_increase_then_delays_increase_exponentially()
    {
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", BindingFlags.NonPublic | BindingFlags.Static);
        _ = method.ShouldNotBeNull();

        var delays = new List<TimeSpan>();

        for(int i = 1; i <= 5; i++)
        {
            var delay = (TimeSpan)method!.Invoke(null, [i])!;
            delays.Add(delay);
        }

        delays[0].TotalSeconds.ShouldBeLessThan(delays[1].TotalSeconds);
        delays[1].TotalSeconds.ShouldBeLessThan(delays[2].TotalSeconds);
        delays[2].TotalSeconds.ShouldBeLessThan(delays[3].TotalSeconds);
        delays[3].TotalSeconds.ShouldBeLessThan(delays[4].TotalSeconds);
    }

    [Fact]
    public void when_attempt_is_10_then_delay_is_capped_at_144_seconds()
    {
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", BindingFlags.NonPublic | BindingFlags.Static);
        _ = method.ShouldNotBeNull();
        var result = (TimeSpan)method!.Invoke(null, [10])!;
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(144);
    }

    [Fact]
    public void when_called_multiple_times_then_delays_have_variability()
    {
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", BindingFlags.NonPublic | BindingFlags.Static);
        _ = method.ShouldNotBeNull();
        var delays = new List<TimeSpan>();
        for(int i = 0; i < 10; i++)
        {
            var delay = (TimeSpan)method!.Invoke(null, [1])!;
            delays.Add(delay);
        }

        int uniqueValues = delays.DistinctBy(d => d.TotalMilliseconds).Count();
        uniqueValues.ShouldBeGreaterThan(1);
    }

    [Theory]
    [InlineData(1, 2.0, 2.4)]      // 2s base, 20% = 0.4s jitter
    [InlineData(2, 4.0, 4.8)]      // 4s base, 20% = 0.8s jitter
    [InlineData(3, 8.0, 9.6)]      // 8s base, 20% = 1.6s jitter
    [InlineData(4, 16.0, 19.2)]    // 16s base, 20% = 3.2s jitter
    [InlineData(5, 32.0, 38.4)]    // 32s base, 20% = 6.4s jitter
    public void when_attempt_is_specified_then_delay_respects_ceiling_and_jitter(int attempt, double minSeconds, double maxSeconds)
    {
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", BindingFlags.NonPublic | BindingFlags.Static);
        _ = method.ShouldNotBeNull();

        var result = (TimeSpan)method!.Invoke(null, [attempt])!;

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(minSeconds);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(maxSeconds);
    }
}
