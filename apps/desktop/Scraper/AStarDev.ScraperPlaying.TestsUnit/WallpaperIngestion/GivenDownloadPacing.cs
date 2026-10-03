using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenDownloadPacing
{
    [Fact]
    public void when_using_the_default_then_every_delay_is_between_two_and_three_seconds()
    {
        var delays = Enumerable.Range(0, 200).Select(_ => DownloadPacing.Default.NextDelay()).ToList();

        delays.ShouldAllBe(delay => delay >= TimeSpan.FromSeconds(2) && delay <= TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void when_using_the_default_then_the_delay_varies()
    {
        var distinctDelays = Enumerable.Range(0, 200).Select(_ => DownloadPacing.Default.NextDelay()).Distinct().Count();

        distinctDelays.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void when_using_none_then_there_is_no_delay() => DownloadPacing.None.NextDelay().ShouldBe(TimeSpan.Zero);
}
