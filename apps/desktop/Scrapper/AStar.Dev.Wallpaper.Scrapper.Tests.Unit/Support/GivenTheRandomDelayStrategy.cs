using System.Diagnostics;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenTheRandomDelayStrategy
{
    private readonly IDelayStrategy sut = new RandomDelayStrategy(new ScrapeConfigurationBuilder().Build());

    [Theory]
    [InlineData(DelayKind.CategoryUpToDate)]
    [InlineData(DelayKind.PageNavigation)]
    [InlineData(DelayKind.ImageAlreadyDownloaded)]
    [InlineData(DelayKind.BeforeImage)]
    [InlineData(DelayKind.Retry)]
    public async Task when_the_cancellation_token_is_already_cancelled_then_delay_async_throws(DelayKind delayKind)
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.DelayAsync(delayKind, cts.Token));
    }

    [Fact]
    public async Task when_page_navigation_is_requested_with_a_zero_second_pause_configured_then_it_can_complete_in_under_two_seconds()
    {
        var zeroPauseStrategy = new RandomDelayStrategy(new ScrapeConfigurationBuilder { SearchConfiguration = new SearchConfigurationBuilder { ImagePauseInSeconds = 0, }.Build(), }.Build());
        var completedUnderTwoSeconds = false;

        for (int attempt = 0; attempt < 10 && !completedUnderTwoSeconds; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            await zeroPauseStrategy.DelayAsync(DelayKind.PageNavigation, TestContext.Current.CancellationToken);
            stopwatch.Stop();

            completedUnderTwoSeconds = stopwatch.Elapsed < TimeSpan.FromSeconds(2);
        }

        completedUnderTwoSeconds.ShouldBeTrue();
    }
}
