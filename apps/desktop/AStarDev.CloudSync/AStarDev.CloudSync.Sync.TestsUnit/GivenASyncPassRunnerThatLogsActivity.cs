using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncPassRunnerThatLogsActivity : IDisposable
{
    private static readonly SyncTrigger Manual = SyncTriggerFactory.CreateManual();

    private readonly SyncRunServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private AccountId Personal => services.Personal;

    private static RelativeSyncPath At(string value) => new(value);

    private static object Outcome(Result<SyncPassSummary, SyncPassFailure> result) => result.Match(_ => (object)"ran", failure => failure);

    private Task<Result<SyncPassSummary, SyncPassFailure>> RunAsync() => services.Runner.RunAsync(Personal, Manual, CancellationToken);

    private void Downloads(params DownloadItemOutcome[] outcomes) => services.Steps.Downloaded = Result.Success<DownloadPassSummary, DownloadPassFailure>(DownloadPassSummaryFactory.Create(outcomes, []));

    private void Uploads(params UploadItemOutcome[] outcomes) => services.Steps.Uploaded = Result.Success<UploadPassSummary, UploadPassFailure>(UploadPassSummaryFactory.Create(outcomes));

    public void Dispose() => services.Dispose();

    [Fact]
    public async Task when_a_run_downloads_uploads_and_deletes_then_one_entry_of_each_kind_is_appended_for_the_account_at_the_run_time()
    {
        Downloads(DownloadItemOutcomeFactory.CreateDownloaded(100), DownloadItemOutcomeFactory.CreateDownloaded(50), DownloadItemOutcomeFactory.CreateDeletedLocally());
        Uploads(UploadItemOutcomeFactory.CreateUploaded(At("a.txt"), 2_000), UploadItemOutcomeFactory.CreateRemovedRemotely(At("b.txt")));

        var result = await RunAsync();

        Outcome(result).ShouldBe("ran");
        services.ActivityLog.Appended.ShouldAllBe(entry => entry.AccountId == Personal && entry.OccurredAt == services.Time.GetUtcNow());
        services.ActivityLog.Appended.Select(entry => entry.Kind).ShouldBe([ActivityKindFactory.CreateDownloaded(2), ActivityKindFactory.CreateUploaded(1, 2_000), ActivityKindFactory.CreateDeleted(2)]);
    }

    [Fact]
    public async Task when_a_run_only_deletes_then_the_deletion_is_still_logged()
    {
        Downloads(DownloadItemOutcomeFactory.CreateDeletedLocally(), DownloadItemOutcomeFactory.CreateDeletedLocally());

        _ = await RunAsync();

        services.ActivityLog.Appended.Select(entry => entry.Kind).ShouldBe([ActivityKindFactory.CreateDeleted(2)]);
    }

    [Fact]
    public async Task when_a_run_changes_nothing_then_nothing_is_appended()
    {
        _ = await RunAsync();

        services.ActivityLog.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_run_fails_then_nothing_is_appended()
    {
        services.Steps.Downloaded = Result.Failure<DownloadPassSummary, DownloadPassFailure>(DownloadPassFailureFactory.CreateRootUnavailable());

        var result = await RunAsync();

        Outcome(result).ShouldBeOfType<SyncPassFailure.RootUnavailable>();
        services.ActivityLog.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_log_cannot_be_written_then_the_run_still_succeeds_and_a_warning_without_details_is_logged()
    {
        services.ActivityLog.Fails = true;
        Downloads(DownloadItemOutcomeFactory.CreateDownloaded(100));

        var result = await RunAsync();

        Outcome(result).ShouldBe("ran");
        string.Join("\n", services.Logs.Logger.Messages).ShouldContain("activity log");
        string.Join("\n", services.Logs.Logger.Messages).ShouldNotContain("locked");
    }

    [Fact]
    public async Task when_the_second_account_runs_then_its_entries_carry_its_own_id()
    {
        Downloads(DownloadItemOutcomeFactory.CreateDownloaded(1));

        _ = await services.Runner.RunAsync(services.Work, Manual, CancellationToken);

        services.ActivityLog.Appended.ShouldHaveSingleItem().AccountId.ShouldBe(services.Work);
    }
}
