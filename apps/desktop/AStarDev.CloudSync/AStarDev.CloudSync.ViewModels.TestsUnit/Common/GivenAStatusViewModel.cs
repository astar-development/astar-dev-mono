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

    [Theory]
    [MemberData(nameof(FailureTexts))]
    public void when_the_last_run_failed_then_the_text_names_the_reason_and_only_the_failed_flag_is_set(SyncFailureReason reason, string text)
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateFailed(reason), TestData.CreateLocalization());

        status.Text.ShouldBe(text);
        status.IsFailed.ShouldBeTrue();
        status.IsUpToDate.ShouldBeFalse();
        status.IsCancelled.ShouldBeFalse();
    }

    public static TheoryData<SyncFailureReason, string> FailureTexts => new()
    {
        { SyncFailureReasonFactory.CreateRootUnavailable(), "Sync folder unavailable" },
        { SyncFailureReasonFactory.CreateQuotaExceeded(), "Cloud storage full" },
        { SyncFailureReasonFactory.CreateInsufficientDiskSpace(), "Not enough disk space" },
        { SyncFailureReasonFactory.CreateUnexpected(), "Sync failed" },
    };

    [Fact]
    public void when_the_last_run_was_cancelled_then_the_text_says_cancelled_and_it_is_not_shown_as_failed()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateCancelled(), TestData.CreateLocalization());

        status.Text.ShouldBe("Sync cancelled");
        status.IsCancelled.ShouldBeTrue();
        status.IsFailed.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1, 0, "1 conflict")]
    [InlineData(3, 0, "3 conflicts")]
    [InlineData(0, 1, "1 deletion held")]
    [InlineData(0, 4, "4 deletions held")]
    [InlineData(2, 5, "2 conflicts, 5 deletions held")]
    public void when_the_last_run_needs_attention_then_the_text_counts_what_waits_and_the_conflicts_flag_is_set(int conflicts, int held, string text)
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateNeedsAttention(conflicts, held), TestData.CreateLocalization());

        status.Text.ShouldBe(text);
        status.HasConflicts.ShouldBeTrue();
    }

    [Fact]
    public void when_the_account_needs_sign_in_then_that_wins_over_a_failed_run()
    {
        var status = new StatusViewModel(SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateUnexpected()), AccountSignInStateFactory.CreateSignInRequired(), TestData.CreateLocalization());

        status.Text.ShouldBe("Sign-in required");
        status.IsFailed.ShouldBeFalse();
    }
}
