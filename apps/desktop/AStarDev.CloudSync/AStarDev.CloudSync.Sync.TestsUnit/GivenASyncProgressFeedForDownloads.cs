using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.DownloadPassFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncProgressFeedForDownloads
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(300);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static byte[] Bytes(int length) => new byte[length];

    private static (long Done, long Total)[] Published(List<SyncProgressChangedEventArgs> events) => [.. events.Select(change => (change.Progress.BytesDone, change.Progress.BytesTotal))];

    private static List<SyncProgressChangedEventArgs> Watch(DownloadPassFixture fixture)
    {
        List<SyncProgressChangedEventArgs> events = [];
        fixture.Feed.ProgressChanged += (_, change) => events.Add(change);

        return events;
    }

    [Fact]
    public async Task when_a_download_reports_progress_over_time_then_the_rising_bytes_are_published()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 100);
        fixture.Downloader.Content["a"] = Bytes(100);
        fixture.Downloader.Reports = [25, 50, 100];
        fixture.Downloader.BeforeEachReport = () => fixture.Time.Advance(Step);
        var events = Watch(fixture);

        _ = await fixture.RunAsync(CancellationToken);

        Published(events).ShouldBe([(25, 100), (50, 100), (100, 100)]);
        events.ShouldAllBe(change => change.Progress.Phase is SyncPhase.Downloading && change.AccountId == fixture.Account);
    }

    [Fact]
    public async Task when_reports_arrive_faster_than_four_a_second_then_only_the_first_is_published()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 100);
        fixture.Downloader.Content["a"] = Bytes(100);
        fixture.Downloader.Reports = [10, 20, 30, 40, 50];
        var events = Watch(fixture);

        _ = await fixture.RunAsync(CancellationToken);

        Published(events).ShouldBe([(10, 100)]);
    }

    [Fact]
    public async Task when_ten_reports_arrive_a_hundred_milliseconds_apart_then_no_more_than_four_are_published_in_the_second()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 100);
        fixture.Downloader.Content["a"] = Bytes(100);
        fixture.Downloader.Reports = [.. Enumerable.Range(1, 10).Select(index => (long)index * 10)];
        fixture.Downloader.BeforeEachReport = () => fixture.Time.Advance(TimeSpan.FromMilliseconds(100));
        var events = Watch(fixture);

        _ = await fixture.RunAsync(CancellationToken);

        Published(events).Select(item => item.Done).ShouldBe([10, 40, 70, 100]);
    }

    [Fact]
    public async Task when_a_provider_reports_nothing_then_the_finished_file_still_counts_in_full()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Downloader.Content["a"] = Bytes(5);
        var events = Watch(fixture);

        _ = await fixture.RunAsync(CancellationToken);

        Published(events).ShouldBe([(5, 5)]);
    }

    [Fact]
    public async Task when_a_download_fails_then_its_bytes_are_not_counted()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 5);
        fixture.Downloader.Failures["a"] = ProviderErrorFactory.CreateTransient();
        var events = Watch(fixture);

        _ = await fixture.RunAsync(CancellationToken);

        events.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_twenty_files_download_at_once_then_the_total_is_their_sum_and_the_feed_stays_consistent()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        foreach (var index in Enumerable.Range(0, 20))
        {
            fixture.Remote.File($"Docs/f{index}.txt", $"f{index}", "c2", 10);
            fixture.Downloader.Content[$"f{index}"] = Bytes(10);
        }

        fixture.Downloader.Reports = [5, 10];
        var events = Watch(fixture);

        var summary = Summary(await Task.Run(() => fixture.RunAsync(CancellationToken), CancellationToken));

        summary.Downloaded.ShouldBe(20);
        events.ShouldHaveSingleItem().Progress.BytesTotal.ShouldBe(200);
    }

    [Fact]
    public async Task when_a_run_is_in_progress_then_the_status_shows_the_percent_of_the_download()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        fixture.Remote.File("Docs/a.txt", "a", "c2", 100);
        fixture.Downloader.Content["a"] = Bytes(100);
        fixture.Downloader.Reports = [50];
        fixture.Steps.OnStep = () => fixture.Time.Advance(Step);
        SyncStatus? seen = null;
        fixture.Downloader.BeforeEachReport = () => fixture.Time.Advance(Step);
        fixture.Feed.ProgressChanged += (_, change) => seen = change.Progress.Phase is SyncPhase.Downloading ? fixture.Status.StatusOf(change.AccountId) : seen;

        _ = await fixture.RunSyncAsync(CancellationToken);

        seen.ShouldBe(SyncStatusFactory.CreateSyncing(50));
    }

    [Fact]
    public async Task when_a_run_ends_then_the_account_has_no_progress()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();

        _ = await fixture.RunSyncAsync(CancellationToken);

        fixture.Feed.ProgressOf(fixture.Account).ShouldBe(Option.None<SyncProgress>());
    }
}
