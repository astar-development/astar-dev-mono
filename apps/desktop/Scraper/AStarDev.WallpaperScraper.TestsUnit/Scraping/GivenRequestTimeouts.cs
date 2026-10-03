using AStarDev.WallpaperScraper.Scraping;

namespace AStarDev.WallpaperScraper.TestsUnit.Scraping;

public sealed class GivenRequestTimeouts
{
    [Fact]
    public async Task when_the_request_succeeds_then_its_result_is_returned()
    {
        var result = await RequestTimeouts.RunAsync(() => Task.FromResult(42), CancellationToken.None);

        result.ShouldBe(42);
    }

    [Fact]
    public async Task when_the_request_is_cancelled_without_the_scrape_being_cancelled_then_a_timeout_is_thrown_carrying_the_original_exception()
    {
        var original = new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.");

        var thrown = await Should.ThrowAsync<TimeoutException>(() => RequestTimeouts.RunAsync<int>(() => throw original, CancellationToken.None));

        thrown.InnerException.ShouldBeSameAs(original);
    }

    [Fact]
    public async Task when_the_scrape_is_cancelled_at_the_same_moment_the_request_times_out_then_it_is_reported_as_a_cancellation()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var thrown = await Should.ThrowAsync<OperationCanceledException>(() => RequestTimeouts.RunAsync<int>(() => throw new TaskCanceledException("timed out"), cancellationTokenSource.Token));

        thrown.ShouldNotBeOfType<TimeoutException>();
    }

    [Fact]
    public async Task when_the_request_fails_for_another_reason_then_that_failure_is_not_changed()
    {
        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => RequestTimeouts.RunAsync<int>(() => throw new InvalidOperationException("boom"), CancellationToken.None));

        thrown.Message.ShouldBe("boom");
    }
}
