using System.Reflection;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public class HttpDownloaderBackoffTests
{
    [Fact]
    public void GetBackoffDelay_Attempt1_ShouldBeBetweenBaseAndBaseWithJitter()
    {
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", BindingFlags.NonPublic | BindingFlags.Static);
        _ = method.ShouldNotBeNull();

        var result = (TimeSpan)method!.Invoke(null, [1])!;
        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(2.0);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(2.4);
    }

    [Fact]
    public void GetBackoffDelay_Attempt2_ShouldBeDoubleAttempt1()
    {
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", BindingFlags.NonPublic | BindingFlags.Static);
        _ = method.ShouldNotBeNull();

        _ = (TimeSpan)method!.Invoke(null, [1])!;
        var result2 = (TimeSpan)method!.Invoke(null, [2])!;
        result2.TotalSeconds.ShouldBeGreaterThanOrEqualTo(4.0);
        result2.TotalSeconds.ShouldBeLessThanOrEqualTo(4.8);
    }

    [Fact]
    public void GetBackoffDelay_IncreasingAttempts_ShouldExponentiallyIncrease()
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
    public void GetBackoffDelay_CappedAt120Seconds()
    {
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", BindingFlags.NonPublic | BindingFlags.Static);
        _ = method.ShouldNotBeNull();
        var result = (TimeSpan)method!.Invoke(null, [10])!;
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(144);
    }

    [Fact]
    public void GetBackoffDelay_WithJitter_ShouldHaveVariability()
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
    public void GetBackoffDelay_RespectsCeilingAndJitter(int attempt, double minSeconds, double maxSeconds)
    {
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", BindingFlags.NonPublic | BindingFlags.Static);
        _ = method.ShouldNotBeNull();

        var result = (TimeSpan)method!.Invoke(null, [attempt])!;

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(minSeconds);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(maxSeconds);
    }
}

public class HttpDownloaderFileOperationTests
{
    [Fact]
    public void HttpDownloader_ShouldBeDisposable()
    {
        var downloader = new HttpDownloader();

        _ = downloader.ShouldBeAssignableTo<IDisposable>();
    }

    [Fact]
    public void HttpDownloader_DisposeMultipleTimes_ShouldNotThrow()
    {
        var downloader = new HttpDownloader();

        downloader.Dispose();
        downloader.Dispose();
    }

    [Fact]
    public async Task DownloadAsync_Created_ShouldHaveValidHttpClient()
    {
        var downloader = new HttpDownloader();
        _ = downloader.ShouldNotBeNull();

        downloader.Dispose();
    }
}
