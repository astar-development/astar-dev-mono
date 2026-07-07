using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Workflows;

public sealed class GivenAPagedScrapeRunner : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var seedContext = new AppDbContext(options);
        await seedContext.Database.MigrateAsync();

        seedContext.ScrapeConfiguration.Add(new ScrapeConfigurationEntity
        {
            ConnectionStrings = new ConnectionStringsEntity { Sqlite = "Data Source=test.db", },
            UserConfiguration = new UserConfigurationEntity { LoginEmailAddress = "user@example.com", Username = "user", Password = "password", SessionCookie = "cookie", },
            SearchConfiguration = new SearchConfigurationEntity { BaseUrl = new Uri("https://example.com"), },
            ScrapeDirectories = new ScrapeDirectoriesEntity { RootDirectory = "root-directory", },
        });
        await seedContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    private sealed class RecordingDelayStrategy(List<string> events) : IDelayStrategy
    {
        public Task DelayAsync(DelayKind delayKind, CancellationToken cancellationToken = default)
        {
            events.Add("delay");

            return Task.CompletedTask;
        }
    }

    private PagedScrapeRunner BuildSut(IDelayStrategy delayStrategy, out IDbContextFactory<AppDbContext> contextFactory)
    {
        contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        var configurationSaver = new ConfigurationSaver(new ScrapeConfigurationBuilder().Build(), new LoggerConfiguration().CreateLogger(), contextFactory);

        return new PagedScrapeRunner(configurationSaver, delayStrategy);
    }

    private static Task<Result<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>> OkUnit() => Task.FromResult(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Value));

    private static Task<Result<IReadOnlyCollection<string>, ScrapeError>> OkLinks() => Task.FromResult(Result.Success<IReadOnlyCollection<string>, ScrapeError>([]));

    [Fact]
    public async Task when_run_then_pages_are_loaded_from_the_starting_page_to_the_total_page_inclusive_in_order()
    {
        List<int> loadedPages = [];
        var plan = PagedScrapePlanFactory.Create(
            1,
            3,
            _ => { },
            page =>
            {
                loadedPages.Add(page);

                return OkUnit();
            },
            OkLinks,
            (_, _) => OkUnit());
        var sut = BuildSut(new NoOpDelayStrategy(), out _);

        await sut.RunAsync(plan, TestContext.Current.CancellationToken);

        loadedPages.ShouldBe([1, 2, 3,]);
    }

    [Fact]
    public async Task when_run_then_each_page_executes_delay_then_record_progress_then_save_then_load_then_get_links_then_process_links_in_order()
    {
        List<string> events = [];
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            events.Add("save");

            return Task.FromResult(new AppDbContext(options));
        });
        var configurationSaver = new ConfigurationSaver(new ScrapeConfigurationBuilder().Build(), new LoggerConfiguration().CreateLogger(), contextFactory);
        var sut = new PagedScrapeRunner(configurationSaver, new RecordingDelayStrategy(events));

        var plan = PagedScrapePlanFactory.Create(
            1,
            2,
            page => events.Add($"record:{page}"),
            page =>
            {
                events.Add($"load:{page}");

                return OkUnit();
            },
            () =>
            {
                events.Add($"links:{events.Count(e => e.StartsWith("load:", StringComparison.Ordinal))}");

                return OkLinks();
            },
            (_, _) =>
            {
                events.Add($"process:{events.Count(e => e.StartsWith("load:", StringComparison.Ordinal))}");

                return OkUnit();
            });

        await sut.RunAsync(plan, TestContext.Current.CancellationToken);

        events.ShouldBe(["delay", "record:1", "save", "load:1", "links:1", "process:1", "delay", "record:2", "save", "load:2", "links:2", "process:2",]);
    }

    [Fact]
    public async Task when_run_then_the_delay_strategy_is_consulted_once_per_page()
    {
        var plan = PagedScrapePlanFactory.Create(1, 3, _ => { }, _ => OkUnit(), OkLinks, (_, _) => OkUnit());
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var sut = BuildSut(delayStrategy, out _);

        await sut.RunAsync(plan, TestContext.Current.CancellationToken);

        await delayStrategy.Received(3).DelayAsync(DelayKind.PageNavigation, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_run_then_the_configuration_is_saved_once_per_page()
    {
        var plan = PagedScrapePlanFactory.Create(1, 3, _ => { }, _ => OkUnit(), OkLinks, (_, _) => OkUnit());
        var sut = BuildSut(new NoOpDelayStrategy(), out var contextFactory);

        await sut.RunAsync(plan, TestContext.Current.CancellationToken);

        await contextFactory.Received(3).CreateDbContextAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_page_load_fails_then_later_pages_are_not_loaded()
    {
        List<int> loadedPages = [];
        var loadFailure = ScrapeErrorFactory.CreatePageLoadFailed("https://example.test/page/2", "navigation failed");
        var plan = PagedScrapePlanFactory.Create(
            1,
            3,
            _ => { },
            page =>
            {
                loadedPages.Add(page);

                return page == 2 ? Task.FromResult(Result.Failure<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(loadFailure)) : OkUnit();
            },
            OkLinks,
            (_, _) => OkUnit());
        var sut = BuildSut(new NoOpDelayStrategy(), out _);

        var result = await sut.RunAsync(plan, TestContext.Current.CancellationToken);

        loadedPages.ShouldBe([1, 2,]);
        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBe(loadFailure);
    }

    [Fact]
    public async Task when_the_get_links_step_fails_then_process_links_is_not_called()
    {
        var linksFailure = ScrapeErrorFactory.CreatePageParseFailed(null, "could not parse links");
        var processLinksCalled = false;
        var plan = PagedScrapePlanFactory.Create(
            1,
            2,
            _ => { },
            _ => OkUnit(),
            () => Task.FromResult(Result.Failure<IReadOnlyCollection<string>, ScrapeError>(linksFailure)),
            (_, _) =>
            {
                processLinksCalled = true;

                return OkUnit();
            });
        var sut = BuildSut(new NoOpDelayStrategy(), out _);

        var result = await sut.RunAsync(plan, TestContext.Current.CancellationToken);

        processLinksCalled.ShouldBeFalse();
        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBe(linksFailure);
    }

    [Fact]
    public async Task when_the_process_links_step_fails_then_the_failure_is_returned_and_later_pages_are_not_loaded()
    {
        List<int> loadedPages = [];
        var processFailure = ScrapeErrorFactory.CreateImageDownloadFailed("https://example.test/image.jpg", "download failed");
        var plan = PagedScrapePlanFactory.Create(
            1,
            2,
            _ => { },
            page =>
            {
                loadedPages.Add(page);

                return OkUnit();
            },
            OkLinks,
            (_, _) => Task.FromResult(Result.Failure<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(processFailure)));
        var sut = BuildSut(new NoOpDelayStrategy(), out _);

        var result = await sut.RunAsync(plan, TestContext.Current.CancellationToken);

        loadedPages.ShouldBe([1,]);
        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBe(processFailure);
    }

    [Fact]
    public async Task when_the_total_page_is_before_the_starting_page_then_the_result_is_a_success_without_any_page_work()
    {
        var pageWorkHappened = false;
        var plan = PagedScrapePlanFactory.Create(
            5,
            1,
            _ => pageWorkHappened = true,
            _ =>
            {
                pageWorkHappened = true;

                return OkUnit();
            },
            () =>
            {
                pageWorkHappened = true;

                return OkLinks();
            },
            (_, _) =>
            {
                pageWorkHappened = true;

                return OkUnit();
            });
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var sut = BuildSut(delayStrategy, out var contextFactory);

        var result = await sut.RunAsync(plan, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
        pageWorkHappened.ShouldBeFalse();
        await delayStrategy.DidNotReceive().DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>());
        await contextFactory.DidNotReceive().CreateDbContextAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_cancellation_token_is_already_cancelled_then_run_async_throws_and_no_page_work_happens()
    {
        var pageWorkHappened = false;
        var plan = PagedScrapePlanFactory.Create(
            1,
            1,
            _ => pageWorkHappened = true,
            _ =>
            {
                pageWorkHappened = true;

                return OkUnit();
            },
            () =>
            {
                pageWorkHappened = true;

                return OkLinks();
            },
            (_, _) =>
            {
                pageWorkHappened = true;

                return OkUnit();
            });
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var sut = BuildSut(delayStrategy, out var contextFactory);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.RunAsync(plan, cts.Token));

        pageWorkHappened.ShouldBeFalse();
        await delayStrategy.DidNotReceive().DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>());
        await contextFactory.DidNotReceive().CreateDbContextAsync(Arg.Any<CancellationToken>());
    }
}
