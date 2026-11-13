using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Ui;
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

    public static TheoryData<SyncFailureReason, string> FailureTexts => new()
    {
        { SyncFailureReasonFactory.CreateCouldNotStart(), "Sync could not start. Try again shortly." },
        { SyncFailureReasonFactory.CreateAuthenticationRequired(), "Sign-in required" },
        { SyncFailureReasonFactory.CreateRootUnavailable(), "Sync folder not found" },
        { SyncFailureReasonFactory.CreateQuotaExceeded(), "Cloud storage is full" },
        { SyncFailureReasonFactory.CreateInsufficientDiskSpace(), "This device is out of disk space" },
        { SyncFailureReasonFactory.CreateCancelled(), "Sync cancelled" },
        { SyncFailureReasonFactory.CreateUnexpected(), "Sync failed. Try again." },
    };

    [Theory]
    [MemberData(nameof(FailureTexts))]
    public void when_the_last_run_failed_then_the_text_says_why(SyncFailureReason reason, string expected)
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateFailed(reason), TestData.CreateLocalization());

        status.Text.ShouldBe(expected);
    }

    [Fact]
    public void when_the_last_run_failed_then_only_the_failed_flag_is_set_and_progress_is_empty()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateQuotaExceeded()), TestData.CreateLocalization());

        (status.IsFailed, status.IsUpToDate, status.IsSyncing, status.HasConflicts, status.IsPaused, status.Progress).ShouldBe((true, false, false, false, false, 0d));
    }

    [Fact]
    public void when_the_last_run_was_cancelled_then_it_is_not_flagged_as_failed()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateCancelled()), TestData.CreateLocalization());

        (status.Text, status.IsFailed).ShouldBe(("Sync cancelled", false));
    }

    [Theory]
    [InlineData(1, 0, "1 conflict")]
    [InlineData(3, 0, "3 conflicts")]
    [InlineData(0, 1, "1 held deletion")]
    [InlineData(0, 61, "61 held deletions")]
    [InlineData(2, 61, "2 conflicts, 61 held deletions")]
    public void when_conflicts_or_held_deletions_wait_then_the_text_counts_them_and_the_attention_flag_is_set(int conflicts, int heldDeletions, string expected)
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateNeedsAttention(conflicts, heldDeletions), TestData.CreateLocalization());

        (status.Text, status.HasConflicts, status.IsUpToDate, status.IsFailed).ShouldBe((expected, true, false, false));
    }

    [Fact]
    public void when_sign_in_is_required_then_a_failed_status_shows_the_sign_in_text_and_no_failed_flag()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateUnexpected()), AccountSignInStateFactory.CreateSignInRequired(), TestData.CreateLocalization());

        (status.Text, status.IsSignInRequired, status.IsFailed).ShouldBe(("Sign-in required", true, false));
    }
}
