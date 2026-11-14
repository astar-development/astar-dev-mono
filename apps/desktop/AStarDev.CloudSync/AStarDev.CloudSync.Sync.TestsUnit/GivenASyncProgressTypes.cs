namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenASyncProgressTypes
{
    [Theory]
    [InlineData(50, 100, 50, 100)]
    [InlineData(150, 100, 100, 100)]
    [InlineData(-5, -1, 0, 0)]
    [InlineData(500, 0, 500, 0)]
    public void when_progress_is_created_then_it_is_normalised(long done, long total, long expectedDone, long expectedTotal)
    {
        var progress = SyncProgressFactory.Create(SyncPhaseFactory.CreateDownloading(), done, total);

        (progress.BytesDone, progress.BytesTotal).ShouldBe((expectedDone, expectedTotal));
    }

    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(1, 3, 33)]
    [InlineData(50, 100, 50)]
    [InlineData(100, 100, 100)]
    [InlineData(40, 0, 0)]
    public void when_the_percent_is_read_then_it_is_the_whole_percent_done_or_zero_without_a_total(long done, long total, int expected) =>
        SyncProgressFactory.Create(SyncPhaseFactory.CreateUploading(), done, total).PercentComplete().ShouldBe(expected);

    [Fact]
    public void when_each_phase_is_created_then_it_is_its_own_case()
    {
        SyncPhase[] phases = [SyncPhaseFactory.CreateUpdatingRemoteIndex(), SyncPhaseFactory.CreateScanningLocal(), SyncPhaseFactory.CreateDownloading(), SyncPhaseFactory.CreateUploading(), SyncPhaseFactory.CreateRecordingConflicts()];

        phases.Select(phase => phase.GetType().Name).ShouldBe(["UpdatingRemoteIndex", "ScanningLocal", "Downloading", "Uploading", "RecordingConflicts"]);
    }
}
