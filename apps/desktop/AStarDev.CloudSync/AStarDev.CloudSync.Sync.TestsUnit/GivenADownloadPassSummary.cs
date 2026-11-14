using static AStarDev.CloudSync.Sync.TestsUnit.SelectionTestRules;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenADownloadPassSummary
{
    private static HeldLocalDeletions Held(int paths) => HeldLocalDeletionsFactory.Create(PairAt("Docs"), Enumerable.Range(0, paths).Select(index => DownloadScenario.At($"Docs/{index}.txt")), 100);

    [Fact]
    public void when_outcomes_are_counted_then_each_kind_has_its_own_count()
    {
        DownloadItemOutcome[] outcomes =
        [
            DownloadItemOutcomeFactory.CreateDownloaded(10),
            DownloadItemOutcomeFactory.CreateDownloaded(5),
            DownloadItemOutcomeFactory.CreateFolderCreated(),
            DownloadItemOutcomeFactory.CreateMoved(),
            DownloadItemOutcomeFactory.CreateDeletedLocally(),
            DownloadItemOutcomeFactory.CreateSentToConflicts(ConflictFindings.Of("Docs/a.txt", ConflictKindFactory.CreateBothAdded())),
            DownloadItemOutcomeFactory.CreateSkipped(DownloadSkipReasonFactory.CreateRemoteGone()),
            DownloadItemOutcomeFactory.CreateFailed(DownloadFailureFactory.CreateLocal(LocalOperationFailureFactory.CreateLocked())),
        ];

        var summary = DownloadPassSummaryFactory.Create(outcomes, []);

        (summary.Downloaded, summary.Bytes, summary.FoldersCreated, summary.Moved, summary.DeletedLocally, summary.SentToConflicts, summary.Skipped, summary.Failed).ShouldBe((2, 15L, 1, 1, 1, 1, 1, 1));
    }

    [Fact]
    public void when_the_guard_held_pairs_then_the_held_items_are_counted_and_the_pairs_are_listed()
    {
        var summary = DownloadPassSummaryFactory.Create([], [Held(60)]);

        summary.HeldByGuard.ShouldBe(60);
        summary.Held.Count.ShouldBe(1);
    }

    [Fact]
    public void when_outcomes_list_held_items_then_they_are_counted_once()
    {
        var summary = DownloadPassSummaryFactory.Create(Enumerable.Repeat(DownloadItemOutcomeFactory.CreateHeldByGuard(), 60), [Held(60)]);

        summary.HeldByGuard.ShouldBe(60);
    }

    [Fact]
    public void when_a_pass_only_deleted_items_then_it_has_changes()
    {
        DownloadPassSummaryFactory.Create([DownloadItemOutcomeFactory.CreateDeletedLocally()], []).HasChanges().ShouldBeTrue();
    }

    [Fact]
    public void when_a_pass_only_moved_items_then_it_has_changes()
    {
        DownloadPassSummaryFactory.Create([DownloadItemOutcomeFactory.CreateMoved()], []).HasChanges().ShouldBeTrue();
    }

    [Fact]
    public void when_a_pass_only_held_deletions_then_it_has_changes()
    {
        DownloadPassSummaryFactory.Create([], [Held(60)]).HasChanges().ShouldBeTrue();
    }

    [Fact]
    public void when_a_pass_only_skipped_and_failed_items_then_it_has_no_changes()
    {
        var summary = DownloadPassSummaryFactory.Create([DownloadItemOutcomeFactory.CreateSkipped(DownloadSkipReasonFactory.CreateLocalBusy()), DownloadItemOutcomeFactory.CreateFailed(DownloadFailureFactory.CreateProvider(ProviderErrorFactory.CreateTransient()))], []);

        summary.HasChanges().ShouldBeFalse();
    }

    [Fact]
    public void when_nothing_happened_then_the_summary_is_empty()
    {
        var summary = DownloadPassSummaryFactory.Create(null!, null!);

        summary.HasChanges().ShouldBeFalse();
        summary.Held.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_size_is_negative_then_it_reads_as_zero()
    {
        DownloadItemOutcomeFactory.CreateDownloaded(-5).ShouldBe(DownloadItemOutcomeFactory.CreateDownloaded(0));
    }

    [Fact]
    public void when_held_paths_repeat_then_they_are_listed_once_and_a_negative_total_reads_as_zero()
    {
        var held = HeldLocalDeletionsFactory.Create(PairAt("Docs"), [DownloadScenario.At("Docs/a.txt"), DownloadScenario.At("DOCS/A.TXT")], -1);

        held.Paths.Count.ShouldBe(1);
        held.Total.ShouldBe(0);
    }

    [Fact]
    public void when_failures_are_created_then_each_factory_makes_its_own_case()
    {
        object[] failures =
        [
            LocalOperationFailureFactory.CreateTargetChanged(),
            LocalOperationFailureFactory.CreateAccessDenied(),
            LocalOperationFailureFactory.CreateLocked(),
            LocalOperationFailureFactory.CreateInsufficientDiskSpace(),
            LocalOperationFailureFactory.CreatePathTooLong(),
            LocalOperationFailureFactory.CreateUnexpected(),
            DownloadPassFailureFactory.CreateRootUnavailable(),
            DownloadPassFailureFactory.CreateInsufficientDiskSpace(),
            DownloadPassFailureFactory.CreateAuthenticationRequired(),
            DownloadPassFailureFactory.CreateUnexpected(),
            DownloadItemOutcomeFactory.CreateHeldByGuard(),
        ];

        failures.Select(failure => failure.GetType().FullName).Distinct().Count().ShouldBe(11);
    }
}
