namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnUploadFollowedByARescan : IDisposable
{
    private const string Content = "0123456789";

    private readonly LocalScanFixture scan = new();
    private readonly UploadPassFixture upload = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        scan.Dispose();
        upload.Dispose();
    }

    [Fact]
    public async Task when_a_file_finished_uploading_before_the_crash_then_the_next_scan_plans_nothing_for_it()
    {
        scan.Include("Docs");
        _ = scan.Write("Docs/done.txt", Content);
        var firstScan = await scan.ScanAsync(CancellationToken);
        _ = upload.FileSystem.Directory.CreateDirectory("/sync/Docs");
        upload.FileSystem.File.WriteAllText("/sync/Docs/done.txt", Content);
        _ = await upload.BaselineFolder("Docs", "docs-id").Changed(firstScan).BuildAsync(CancellationToken);
        _ = await upload.RunAsync(CancellationToken);
        foreach (var entry in upload.Baseline.All)
            scan.Baseline.Add(entry);

        var secondScan = await scan.ScanAsync(CancellationToken);

        (LocalScanFixture.Describe(firstScan), upload.Uploader.Calls.Count, LocalScanFixture.Describe(secondScan)).ShouldBe(("Added:Docs/done.txt:File", 1, string.Empty));
    }

    [Fact]
    public async Task when_the_upload_never_recorded_then_the_next_scan_plans_the_file_again()
    {
        scan.Include("Docs");
        _ = scan.Write("Docs/lost.txt", Content);
        var firstScan = await scan.ScanAsync(CancellationToken);
        _ = upload.FileSystem.Directory.CreateDirectory("/sync/Docs");
        upload.FileSystem.File.WriteAllText("/sync/Docs/lost.txt", Content);
        _ = await upload.BaselineFolder("Docs", "docs-id").Changed(firstScan).BuildAsync(CancellationToken);
        upload.Recorder.Fails = true;
        _ = await upload.RunAsync(CancellationToken);
        foreach (var entry in upload.Baseline.All)
            scan.Baseline.Add(entry);

        var secondScan = await scan.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(secondScan).ShouldBe("Added:Docs/lost.txt:File");
    }
}
