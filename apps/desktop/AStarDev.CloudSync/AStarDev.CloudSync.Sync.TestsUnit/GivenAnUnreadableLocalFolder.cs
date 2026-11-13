using AStarDev.FunctionalParadigm;
using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnUnreadableLocalFolder : IDisposable
{
    private readonly LocalScanFixture fixture = new(windows: true);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => fixture.Dispose();

    [Fact]
    public async Task when_a_folder_cannot_be_listed_then_its_baseline_items_are_unreadable_and_never_deleted()
    {
        fixture.Include("Docs");
        _ = fixture.SyncedFolder("Docs/Locked", "item-locked");
        _ = fixture.Synced("Docs/Locked/kept.txt", "kept", "item-kept");
        _ = fixture.Synced("Docs/Locked/gone.txt", "gone", "item-gone");
        fixture.FileSystem.File.Delete($"{fixture.Root}/Docs/Locked/gone.txt");
        fixture.Deny("Docs/Locked");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Unreadable:Docs/Locked;Unreadable:Docs/Locked/gone.txt;Unreadable:Docs/Locked/kept.txt");
    }

    [Fact]
    public async Task when_a_folder_cannot_be_listed_then_the_rest_of_the_scan_still_reports_its_changes()
    {
        fixture.Include("Docs");
        _ = fixture.SyncedFolder("Docs/Locked", "item-locked");
        _ = fixture.Write("Docs/new.txt", "hello");
        fixture.Deny("Docs/Locked");

        var set = await fixture.ScanAsync(CancellationToken);

        LocalScanFixture.Describe(set).ShouldBe("Unreadable:Docs/Locked;Added:Docs/new.txt:File");
    }

    [Fact]
    public async Task when_an_unlistable_folder_holds_many_baseline_items_then_they_do_not_trip_the_mass_deletion_guard()
    {
        fixture.Include("Docs");
        fixture.SyncedFiles("Docs/Locked", 60);
        fixture.Lose("Docs/Locked", 60);
        fixture.Deny("Docs/Locked");

        var set = await fixture.ScanAsync(CancellationToken);

        (set.Holds.Count, set.Changes.OfType<LocalChange.Deleted>().Count(), set.Summary.Unreadable).ShouldBe((0, 0, 61));
    }

    [Fact]
    public async Task when_the_root_cannot_be_listed_then_the_scan_reports_the_root_unavailable()
    {
        fixture.Include("Docs");
        _ = fixture.Synced("Docs/a.txt", "same", "item-a");
        fixture.Deny(string.Empty);

        var result = await fixture.Detector.DetectAsync(LocalScanFixture.Account, CancellationToken);

        result.Match(_ => "scanned", failure => failure.GetType().Name).ShouldBe(nameof(LocalScanFailure.RootUnavailable));
    }
}
