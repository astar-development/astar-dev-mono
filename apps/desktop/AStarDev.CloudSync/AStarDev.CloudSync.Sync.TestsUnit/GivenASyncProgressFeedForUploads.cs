namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncProgressFeedForUploads : IDisposable
{
    private readonly UploadPassFixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_an_upload_reports_progress_over_time_then_the_rising_bytes_are_published()
    {
        _ = await fixture.BaselineFolder("Docs", "docs-id").AddedFile("Docs/a.txt").BuildAsync(CancellationToken);
        fixture.Uploader.Reports = [4, 10];
        fixture.Uploader.BeforeEachReport = () => fixture.Time.Advance(TimeSpan.FromMilliseconds(300));
        List<SyncProgressChangedEventArgs> events = [];
        fixture.Feed.ProgressChanged += (_, change) => events.Add(change);

        _ = await fixture.RunAsync(CancellationToken);

        events.Select(change => (change.Progress.BytesDone, change.Progress.BytesTotal)).ShouldBe([(4, 10), (10, 10)]);
        events.ShouldAllBe(change => change.Progress.Phase is SyncPhase.Uploading);
    }

    [Fact]
    public async Task when_an_upload_reports_nothing_then_the_finished_file_still_counts_in_full()
    {
        _ = await fixture.BaselineFolder("Docs", "docs-id").AddedFile("Docs/a.txt").BuildAsync(CancellationToken);
        List<SyncProgressChangedEventArgs> events = [];
        fixture.Feed.ProgressChanged += (_, change) => events.Add(change);

        _ = await fixture.RunAsync(CancellationToken);

        events.Select(change => (change.Progress.BytesDone, change.Progress.BytesTotal)).ShouldBe([(10, 10)]);
    }

    public void Dispose() => fixture.Dispose();
}
