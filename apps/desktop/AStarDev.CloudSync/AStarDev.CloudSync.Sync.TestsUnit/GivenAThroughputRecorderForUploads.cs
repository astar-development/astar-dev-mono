using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAThroughputRecorderForUploads : IDisposable
{
    private readonly UploadPassFixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_a_file_finishes_uploading_then_one_sample_carries_its_bytes_and_the_time_it_took()
    {
        _ = await fixture.BaselineFolder("Docs", "docs-id").AddedFile("Docs/a.txt").BuildAsync(CancellationToken);
        fixture.Uploader.Reports = [4, 10];
        fixture.Uploader.BeforeEachReport = () => fixture.Time.Advance(TimeSpan.FromMilliseconds(300));

        _ = await fixture.RunAsync(CancellationToken);

        fixture.Throughput.Samples.ShouldBe([TransferSampleFactory.Create(fixture.Time.GetUtcNow(), 10, TimeSpan.FromMilliseconds(300))]);
    }

    [Fact]
    public async Task when_a_file_reports_no_progress_then_its_bytes_are_still_sampled_at_once()
    {
        _ = await fixture.BaselineFolder("Docs", "docs-id").AddedFile("Docs/a.txt").BuildAsync(CancellationToken);

        _ = await fixture.RunAsync(CancellationToken);

        fixture.Throughput.Samples.ShouldBe([TransferSampleFactory.Create(fixture.Time.GetUtcNow(), 10, TimeSpan.Zero)]);
    }

    [Fact]
    public async Task when_a_transfer_runs_longer_than_ten_seconds_then_a_sample_is_written_every_ten_seconds_and_when_the_phase_finishes()
    {
        _ = await fixture.BaselineFolder("Docs", "docs-id").AddedFile("Docs/a.txt").BuildAsync(CancellationToken);
        var start = fixture.Time.GetUtcNow();
        fixture.Uploader.Reports = [2, 4, 6, 10];
        fixture.Uploader.BeforeEachReport = () => fixture.Time.Advance(TimeSpan.FromSeconds(6));

        _ = await fixture.RunAsync(CancellationToken);

        fixture.Throughput.Samples.ShouldBe([TransferSampleFactory.Create(start.AddSeconds(18), 6, TimeSpan.FromSeconds(12)), TransferSampleFactory.Create(start.AddSeconds(24), 4, TimeSpan.Zero)]);
    }

    [Fact]
    public async Task when_the_sample_cannot_be_written_then_the_upload_still_succeeds()
    {
        _ = await fixture.BaselineFolder("Docs", "docs-id").AddedFile("Docs/a.txt").BuildAsync(CancellationToken);
        fixture.Throughput.Fails = true;

        var result = await fixture.RunAsync(CancellationToken);

        result.Match(_ => "uploaded", _ => "failed").ShouldBe("uploaded");
    }

    public void Dispose() => fixture.Dispose();
}
