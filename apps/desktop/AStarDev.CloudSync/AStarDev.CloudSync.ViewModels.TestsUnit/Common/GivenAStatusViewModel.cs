using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.ViewModels.Common;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Common;

public class GivenAStatusViewModel
{
    [Fact]
    public void when_syncing_then_the_text_shows_the_percentage_and_only_the_syncing_flag_is_set()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateSyncing(25), TestData.CreateLocalization());

        status.Text.ShouldBe("Syncing 25%");
        status.IsSyncing.ShouldBeTrue();
        status.IsUpToDate.ShouldBeFalse();
        status.HasConflicts.ShouldBeFalse();
        status.IsPaused.ShouldBeFalse();
    }

    [Fact]
    public void when_up_to_date_then_the_text_says_so()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateUpToDate(), TestData.CreateLocalization());

        status.Text.ShouldBe("Up to date");
        status.IsUpToDate.ShouldBeTrue();
    }

    [Fact]
    public void when_there_is_one_conflict_then_the_text_is_singular()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateHasConflicts(1), TestData.CreateLocalization());

        status.Text.ShouldBe("1 conflict");
        status.HasConflicts.ShouldBeTrue();
    }

    [Fact]
    public void when_there_are_several_conflicts_then_the_text_is_plural()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateHasConflicts(2), TestData.CreateLocalization());

        status.Text.ShouldBe("2 conflicts");
    }

    [Fact]
    public void when_paused_then_the_text_says_so()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreatePaused(), TestData.CreateLocalization());

        status.Text.ShouldBe("Paused");
        status.IsPaused.ShouldBeTrue();
    }

    [Fact]
    public void when_syncing_then_the_progress_is_the_percentage_as_a_fraction()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateSyncing(25), TestData.CreateLocalization());

        status.Progress.ShouldBe(0.25);
    }

    [Fact]
    public void when_not_syncing_then_the_progress_is_complete()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateUpToDate(), TestData.CreateLocalization());

        status.Progress.ShouldBe(1);
    }

    [Fact]
    public void when_paused_then_the_progress_is_empty()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreatePaused(), TestData.CreateLocalization());

        status.Progress.ShouldBe(0);
    }
}
