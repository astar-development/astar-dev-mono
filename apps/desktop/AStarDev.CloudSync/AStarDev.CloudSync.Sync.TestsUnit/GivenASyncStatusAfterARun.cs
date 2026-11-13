using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenASyncStatusAfterARun
{
    public static TheoryData<SyncPassFailure, SyncFailureReason> Failures => new()
    {
        { SyncPassFailureFactory.CreateAlreadyRunning(), SyncFailureReasonFactory.CreateCouldNotStart() },
        { SyncPassFailureFactory.CreateAuthenticationRequired(), SyncFailureReasonFactory.CreateAuthenticationRequired() },
        { SyncPassFailureFactory.CreateRootUnavailable(), SyncFailureReasonFactory.CreateRootUnavailable() },
        { SyncPassFailureFactory.CreateQuotaExceeded(), SyncFailureReasonFactory.CreateQuotaExceeded() },
        { SyncPassFailureFactory.CreateInsufficientDiskSpace(), SyncFailureReasonFactory.CreateInsufficientDiskSpace() },
        { SyncPassFailureFactory.CreateCancelled(), SyncFailureReasonFactory.CreateCancelled() },
        { SyncPassFailureFactory.CreateUnexpected(), SyncFailureReasonFactory.CreateUnexpected() },
    };

    [Theory]
    [MemberData(nameof(Failures))]
    public void when_a_run_fails_then_the_status_is_failed_with_the_matching_reason(SyncPassFailure failure, SyncFailureReason reason)
    {
        var status = SyncStatusFactory.CreateFromRun(Result.Failure<SyncPassSummary, SyncPassFailure>(failure));

        status.ShouldBe(SyncStatusFactory.CreateFailed(reason));
    }

    [Fact]
    public void when_a_run_finishes_with_nothing_waiting_then_the_status_is_up_to_date()
    {
        var summary = SyncPassSummaryFactory.Create(RemoteUpdateSummaryFactory.Create(RelevantChangeCountsFactory.None, RuleRelocationSummaryFactory.None, Option.None<ResyncKind>(), 1), LocalChangeSetFactory.Create([], 0, 0), DownloadPassSummaryFactory.Create([], []), UploadPassSummaryFactory.Create([]));

        var status = SyncStatusFactory.CreateFromRun(Result.Success<SyncPassSummary, SyncPassFailure>(summary));

        status.ShouldBe(SyncStatusFactory.CreateUpToDate());
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-3, 0)]
    [InlineData(0, -1)]
    public void when_no_conflicts_and_no_held_deletions_wait_then_needs_attention_becomes_up_to_date(int conflicts, int heldDeletions)
    {
        var status = SyncStatusFactory.CreateNeedsAttention(conflicts, heldDeletions);

        status.ShouldBe(SyncStatusFactory.CreateUpToDate());
    }

    [Fact]
    public void when_only_held_deletions_wait_then_needs_attention_keeps_the_count_and_a_negative_conflict_count_is_zero()
    {
        var status = SyncStatusFactory.CreateNeedsAttention(-2, 7);

        status.ShouldBe(SyncStatusFactory.CreateNeedsAttention(0, 7));
        status.ShouldBeOfType<SyncStatus.NeedsAttention>().HeldDeletions.ShouldBe(7);
    }
}
