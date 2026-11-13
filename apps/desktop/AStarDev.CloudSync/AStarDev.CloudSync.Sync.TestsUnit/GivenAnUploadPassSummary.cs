using static AStarDev.CloudSync.Sync.TestsUnit.UploadPlanFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAnUploadPassSummary
{
    [Fact]
    public void when_outcomes_of_every_kind_are_counted_then_each_count_and_the_bytes_are_reported()
    {
        var summary = UploadPassSummaryFactory.Create([
            UploadItemOutcomeFactory.CreateUploaded(At("a.txt"), 100),
            UploadItemOutcomeFactory.CreateUploaded(At("b.txt"), 50),
            UploadItemOutcomeFactory.CreateFolderCreated(At("New")),
            UploadItemOutcomeFactory.CreateLinked(At("Old")),
            UploadItemOutcomeFactory.CreateRemovedRemotely(At("c.txt")),
            UploadItemOutcomeFactory.CreateSentToConflicts(At("d.txt"), ConflictKindFactory.CreateBothModified()),
            UploadItemOutcomeFactory.CreateDeferred(At("e.txt")),
            UploadItemOutcomeFactory.CreateFailed(At("f.txt"), ProviderErrorFactory.CreateTransient()),
            UploadItemOutcomeFactory.CreateRejected(At("g:h.txt"), InvalidRemoteNameFactory.CreateIllegalCharacter()),
        ]);

        (summary.Uploaded, summary.BytesUploaded, summary.FoldersCreated, summary.Linked, summary.Removed, summary.SentToConflicts, summary.Deferred, summary.Failed, summary.Rejected.Count).ShouldBe((2, 150L, 1, 1, 1, 1, 1, 1, 1));
    }

    [Fact]
    public void when_a_pass_only_deletes_then_the_removals_are_counted_so_it_does_not_read_as_no_changes()
    {
        var summary = UploadPassSummaryFactory.Create([UploadItemOutcomeFactory.CreateRemovedRemotely(At("a.txt")), UploadItemOutcomeFactory.CreateRemovedRemotely(At("b.txt"))]);

        (summary.Removed, summary.Uploaded).ShouldBe((2, 0));
    }

    [Fact]
    public void when_a_name_is_rejected_then_the_reason_is_kept_for_the_user()
    {
        var summary = UploadPassSummaryFactory.Create([UploadItemOutcomeFactory.CreateRejected(At("con"), InvalidRemoteNameFactory.CreateReservedName())]);

        summary.Rejected.ShouldHaveSingleItem().Reason.ShouldBe(InvalidRemoteNameFactory.CreateReservedName());
    }

    [Fact]
    public void when_every_failure_case_is_created_then_each_is_a_distinct_case()
    {
        UploadPassFailure[] failures = [UploadPassFailureFactory.CreateRootUnavailable(), UploadPassFailureFactory.CreateQuotaExceeded(), UploadPassFailureFactory.CreateAuthenticationRequired(), UploadPassFailureFactory.CreateUnexpected()];

        failures.Select(failure => failure.GetType().Name).Distinct().Count().ShouldBe(4);
    }

    [Fact]
    public void when_every_invalid_name_case_is_created_then_each_is_a_distinct_case()
    {
        InvalidRemoteName[] reasons = [InvalidRemoteNameFactory.CreateIllegalCharacter(), InvalidRemoteNameFactory.CreateLeadingOrTrailingSpace(), InvalidRemoteNameFactory.CreateEndsWithDot(), InvalidRemoteNameFactory.CreateReservedName(), InvalidRemoteNameFactory.CreatePathTooLong(), InvalidRemoteNameFactory.CreateFileTooLarge()];

        reasons.Select(reason => reason.GetType().Name).Distinct().Count().ShouldBe(6);
    }
}
