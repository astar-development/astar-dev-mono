using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncNotificationPolicy : IDisposable
{
    private static readonly WindowPresence Away = WindowPresenceFactory.CreateNotInFront();
    private readonly SyncRunServices services = new();

    public void Dispose() => services.Dispose();

    private static Result<SyncPassSummary, SyncPassFailure> Ran(IEnumerable<DownloadItemOutcome>? downloads = null, IEnumerable<UploadItemOutcome>? uploads = null) =>
        Result.Success<SyncPassSummary, SyncPassFailure>(SyncPassSummaryFactory.Create(
            RemoteUpdateSummaryFactory.Create(RelevantChangeCountsFactory.None, RuleRelocationSummaryFactory.None, Option.None<ResyncKind>(), 1),
            LocalChangeSetFactory.Create([], 0, 0),
            DownloadPassSummaryFactory.Create(downloads ?? [], []),
            UploadPassSummaryFactory.Create(uploads ?? [])));

    private static Result<SyncPassSummary, SyncPassFailure> Failed(SyncPassFailure failure) => Result.Failure<SyncPassSummary, SyncPassFailure>(failure);

    private SyncNotification? Decide(Result<SyncPassSummary, SyncPassFailure> outcome, WindowPresence? presence = null) =>
        services.Notifications.Decide(outcome, presence ?? Away).Match(notification => (SyncNotification?)notification, () => null);

    [Fact]
    public void when_the_window_is_in_front_then_nothing_is_sent_for_any_outcome()
    {
        var front = WindowPresenceFactory.CreateInFront();
        var changed = Ran([DownloadItemOutcomeFactory.CreateDownloaded(10)]);

        (Decide(changed, front), Decide(Failed(SyncPassFailureFactory.CreateUnexpected()), front), Decide(Failed(SyncPassFailureFactory.CreateAuthenticationRequired()), front)).ShouldBe((null, null, null));
    }

    [Fact]
    public void when_a_pass_changed_files_then_the_count_of_downloads_uploads_and_deletions_is_reported()
    {
        var outcome = Ran(
            [DownloadItemOutcomeFactory.CreateDownloaded(10), DownloadItemOutcomeFactory.CreateDownloaded(20), DownloadItemOutcomeFactory.CreateDeletedLocally()],
            [UploadItemOutcomeFactory.CreateUploaded(new RelativeSyncPath("a.txt"), 5), UploadItemOutcomeFactory.CreateRemovedRemotely(new RelativeSyncPath("b.txt"))]);

        Decide(outcome).ShouldBe(SyncNotificationFactory.CreateSynced(5));
    }

    [Fact]
    public void when_a_pass_changed_nothing_then_nothing_is_sent()
    {
        Decide(Ran()).ShouldBeNull();
    }

    [Fact]
    public void when_a_pass_found_conflicts_then_the_conflicts_are_reported_before_the_changes()
    {
        var outcome = Ran(
            [DownloadItemOutcomeFactory.CreateDownloaded(10), DownloadItemOutcomeFactory.CreateSentToConflicts(ConflictFindings.Of("a.txt", ConflictKindFactory.CreateBothModified()))],
            [UploadItemOutcomeFactory.CreateSentToConflicts(ConflictFindings.Of("b.txt", ConflictKindFactory.CreateBothAdded()))]);

        Decide(outcome).ShouldBe(SyncNotificationFactory.CreateConflictsFound(2));
    }

    [Fact]
    public void when_a_pass_held_deletions_then_that_is_reported()
    {
        var outcome = Ran([DownloadItemOutcomeFactory.CreateHeldByGuard(), DownloadItemOutcomeFactory.CreateHeldByGuard(), DownloadItemOutcomeFactory.CreateHeldByGuard()]);

        Decide(outcome).ShouldBe(SyncNotificationFactory.CreateDeletionsHeld(3));
    }

    [Fact]
    public void when_the_account_must_sign_in_then_that_is_reported()
    {
        Decide(Failed(SyncPassFailureFactory.CreateAuthenticationRequired())).ShouldBe(SyncNotificationFactory.CreateSignInRequired());
    }

    [Theory]
    [InlineData("RootUnavailable")]
    [InlineData("QuotaExceeded")]
    [InlineData("InsufficientDiskSpace")]
    [InlineData("DatabaseUnavailable")]
    [InlineData("Unexpected")]
    public void when_a_pass_failed_for_another_reason_then_a_failure_is_reported(string failure)
    {
        var reason = failure switch
        {
            "RootUnavailable" => SyncPassFailureFactory.CreateRootUnavailable(),
            "QuotaExceeded" => SyncPassFailureFactory.CreateQuotaExceeded(),
            "InsufficientDiskSpace" => SyncPassFailureFactory.CreateInsufficientDiskSpace(),
            "DatabaseUnavailable" => SyncPassFailureFactory.CreateDatabaseUnavailable(),
            _ => SyncPassFailureFactory.CreateUnexpected(),
        };

        Decide(Failed(reason)).ShouldBe(SyncNotificationFactory.CreateSyncFailed());
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("AlreadyRunning")]
    public void when_a_pass_was_cancelled_or_skipped_then_nothing_is_sent(string failure)
    {
        var reason = failure == "Cancelled" ? SyncPassFailureFactory.CreateCancelled() : SyncPassFailureFactory.CreateAlreadyRunning();

        Decide(Failed(reason)).ShouldBeNull();
    }
}
