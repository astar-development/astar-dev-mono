using AStarDev.CloudSync.Conflicts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Conflicts;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Snapshots;

public class GivenTheConflictListSnapshots
{
    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_the_queue_is_empty_then_the_list_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var window = await CreateWindowAsync(new FakeAttentionReader(), new FakeConflictResolver(), AttentionItems.Account("Photos"));

        Snapshot.ShouldMatchBaseline(window, Theme(theme), $"conflict-list-empty-{theme}");
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_the_queue_is_mixed_then_the_list_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var photos = AttentionItems.Account("Photos");
        var work = AttentionItems.Account("Work");
        var reader = new FakeAttentionReader();
        reader.Add(photos.Id, AttentionItems.CreateConflict(photos.Id, "Camera/holiday-042.jpg"), AttentionItems.CreateConflict(photos.Id, "Camera/holiday-043.jpg", ConflictKindFactory.CreateBothAdded()));
        reader.Add(work.Id, AttentionItems.CreateConflict(work.Id, "Docs/budget.xlsx", ConflictKindFactory.CreateLocalModifiedRemoteDeleted(), AttentionItems.Present(), SideSnapshotFactory.CreateAbsent()), AttentionItems.CreateConflict(work.Id, "Docs/plan.docx", ConflictKindFactory.CreateLocalDeletedRemoteModified(), SideSnapshotFactory.CreateAbsent(), AttentionItems.Present()));
        var window = await CreateWindowAsync(reader, new FakeConflictResolver(), photos, work);

        Snapshot.ShouldMatchBaseline(window, Theme(theme), $"conflict-list-mixed-{theme}");
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public async Task when_an_item_failed_then_the_list_shows_its_error_and_matches_the_baseline(string theme)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), Snapshot.LinuxOnlyReason);
        var photos = AttentionItems.Account("Photos");
        var reader = new FakeAttentionReader();
        var failing = (SyncAttentionItem.Conflict)AttentionItems.CreateConflict(photos.Id, "Camera/holiday-042.jpg");
        reader.Add(photos.Id, failing, AttentionItems.CreateConflict(photos.Id, "Camera/holiday-043.jpg"));
        var resolver = new FakeConflictResolver { Outcome = (_, _, _, _) => Task.FromResult(Result.Success<ResolutionSummary, ErrorResponse>(ResolutionSummaryFactory.Create(0, [FailedResolutionFactory.Create(failing.Id, ResolutionFailureFactory.CreateRemoteChanged())], 0))) };
        var window = await CreateWindowAsync(reader, resolver, photos);
        var list = (AStarDev.CloudSync.ViewModels.Conflicts.ConflictListViewModel)((ConflictListView)window.Content!).DataContext!;

        await list.Groups.Single().Items[0].Options[0].Command.ExecuteAsync(null);

        Snapshot.ShouldMatchBaseline(window, Theme(theme), $"conflict-list-failed-{theme}");
    }

    private static ThemeVariant Theme(string theme) => theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static async Task<Window> CreateWindowAsync(FakeAttentionReader reader, FakeConflictResolver resolver, params CloudAccount[] accounts)
    {
        var list = TestData.CreateConflictList(new FixedAccountsSource(accounts), reader, resolver);
        await list.LoadAsync(TestContext.Current.CancellationToken);

        return new Window { Content = new ConflictListView { DataContext = list } };
    }
}
