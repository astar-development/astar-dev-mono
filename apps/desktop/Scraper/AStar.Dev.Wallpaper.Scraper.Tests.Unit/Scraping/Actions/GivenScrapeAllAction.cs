using AStar.Dev.FunctionalParadigm;
using Microsoft.Playwright;
using AStar.Dev.Wallpaper.Scraper.Scraping.Actions;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping.Actions;

public sealed class GivenScrapeAllAction
{
    private readonly IScrapeAction searchCategoryScrapeAction = Substitute.For<IScrapeAction>();
    private readonly ITopWallpapersScrapeAction topWallpapersScrapeAction = Substitute.For<ITopWallpapersScrapeAction>();
    private readonly ISubscriptionsScrapeAction subscriptionsScrapeAction = Substitute.For<ISubscriptionsScrapeAction>();
    private readonly IProgress<string> progress = Substitute.For<IProgress<string>>();
    private readonly IPage page = Substitute.For<IPage>();

    [Fact]
    public async Task when_all_steps_succeed_then_each_action_executes_against_the_configured_page_and_a_success_result_is_returned()
    {
        var sut = CreateSut();

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        await searchCategoryScrapeAction.Received().ExecuteAsync(page, progress, Arg.Any<CancellationToken>());
        await topWallpapersScrapeAction.Received().ExecuteAsync(page, progress, Arg.Any<CancellationToken>());
        await subscriptionsScrapeAction.Received().ExecuteAsync(page, progress, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_search_categories_step_fails_then_the_failure_is_reported_and_the_remaining_steps_still_run()
    {
        var sut = CreateSut();
        searchCategoryScrapeAction.ExecuteAsync(page, progress, Arg.Any<CancellationToken>())
            .Returns(Exceptional.Failure<FunctionalParadigm.UnitFp>(new InvalidOperationException("search categories boom")));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Scrape Search Categories") && message.Contains("search categories boom")));
        await topWallpapersScrapeAction.Received().ExecuteAsync(page, progress, Arg.Any<CancellationToken>());
        await subscriptionsScrapeAction.Received().ExecuteAsync(page, progress, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_top_wallpapers_step_fails_then_the_failure_is_reported_and_subscriptions_still_runs()
    {
        var sut = CreateSut();
        topWallpapersScrapeAction.ExecuteAsync(page, progress, Arg.Any<CancellationToken>())
            .Returns(Exceptional.Failure<FunctionalParadigm.UnitFp>(new InvalidOperationException("top wallpapers boom")));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Scrape Top Wallpapers") && message.Contains("top wallpapers boom")));
        await subscriptionsScrapeAction.Received().ExecuteAsync(page, progress, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_cancellation_is_requested_before_the_run_starts_then_the_operation_is_cancelled_and_no_step_executes()
    {
        var sut = CreateSut();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(sut.ExecuteAsync(page, progress, cancellationSource.Token));

        await searchCategoryScrapeAction.DidNotReceive().ExecuteAsync(Arg.Any<IPage>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    private ScrapeAllAction CreateSut()
    {
        searchCategoryScrapeAction.Name.Returns("Scrape Search Categories");
        topWallpapersScrapeAction.Name.Returns("Scrape Top Wallpapers");
        subscriptionsScrapeAction.Name.Returns("Scrape Subscribed Wallpapers");

        searchCategoryScrapeAction.ExecuteAsync(Arg.Any<IPage>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(Exceptional.Success(FunctionalParadigm.UnitFp.Instance));
        topWallpapersScrapeAction.ExecuteAsync(Arg.Any<IPage>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(Exceptional.Success(FunctionalParadigm.UnitFp.Instance));
        subscriptionsScrapeAction.ExecuteAsync(Arg.Any<IPage>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(Exceptional.Success(FunctionalParadigm.UnitFp.Instance));

        return new(searchCategoryScrapeAction, topWallpapersScrapeAction, subscriptionsScrapeAction);
    }
}
