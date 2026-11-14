using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASyncPassRunnerThatRefreshesQuota : IDisposable
{
    private static readonly SyncTrigger Manual = SyncTriggerFactory.CreateManual();
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private readonly SyncRunServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private AccountId Personal => services.Personal;

    private static object Outcome(Result<SyncPassSummary, SyncPassFailure> result) => result.Match(_ => (object)"ran", failure => failure);

    [Fact]
    public async Task when_a_pass_finishes_then_the_account_quota_is_refreshed_once()
    {
        var result = await services.Runner.RunAsync(Personal, Manual, CancellationToken);

        Outcome(result).ShouldBe("ran");
        services.Quota.Calls.ShouldBe([Personal]);
    }

    [Fact]
    public async Task when_a_pass_finishes_then_the_quota_is_refreshed_before_the_finished_event_is_raised()
    {
        var callsAtFinish = new List<int>();
        services.Activity.PassFinished += (_, _) => callsAtFinish.Add(services.Quota.Calls.Count);

        _ = await services.Runner.RunAsync(Personal, Manual, CancellationToken);

        callsAtFinish.ShouldBe([1]);
    }

    [Fact]
    public async Task when_a_pass_fails_for_another_reason_then_the_quota_is_still_refreshed()
    {
        services.Steps.Uploaded = Result.Failure<UploadPassSummary, UploadPassFailure>(UploadPassFailureFactory.CreateQuotaExceeded());

        var result = await services.Runner.RunAsync(Personal, Manual, CancellationToken);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateQuotaExceeded());
        services.Quota.Calls.ShouldBe([Personal]);
    }

    [Fact]
    public async Task when_the_quota_refresh_fails_then_the_run_outcome_is_unchanged()
    {
        services.Quota.Outcome = Result.Failure<StorageUsage, ProviderError>(ProviderErrorFactory.CreateTransient());

        var result = await services.Runner.RunAsync(Personal, Manual, CancellationToken);

        Outcome(result).ShouldBe("ran");
    }

    [Fact]
    public async Task when_the_quota_refresh_throws_then_the_run_outcome_is_unchanged_and_the_account_is_freed()
    {
        services.Quota.Throws = Option.Some<Exception>(new InvalidOperationException("jane@example.com"));

        var result = await services.Runner.RunAsync(Personal, Manual, CancellationToken);

        Outcome(result).ShouldBe("ran");
        services.SyncLock.TryAcquire(Personal).ShouldBeOfType<SyncLockAttempt.Acquired>();
    }

    [Fact]
    public async Task when_the_pass_is_cancelled_then_the_quota_is_not_refreshed()
    {
        var gate = new WriteGate();
        services.Steps.DownloadGate = Option.Some(gate);
        var running = services.Runner.RunAsync(Personal, Manual, CancellationToken);
        await gate.Entered.WaitAsync(Limit, CancellationToken);

        await services.Cancel.CancelAsync(Personal, CancellationToken).WaitAsync(Limit, CancellationToken);

        Outcome(await running.WaitAsync(Limit, CancellationToken)).ShouldBe(SyncPassFailureFactory.CreateCancelled());
        services.Quota.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_run_is_skipped_because_one_is_in_progress_then_it_does_not_refresh_the_quota()
    {
        using var held = services.SyncLock.TryAcquire(Personal).ShouldBeOfType<SyncLockAttempt.Acquired>().Lease;

        var result = await services.Runner.RunAsync(Personal, Manual, CancellationToken);

        Outcome(result).ShouldBe(SyncPassFailureFactory.CreateAlreadyRunning());
        services.Quota.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_many_accounts_finish_at_once_then_each_account_is_refreshed_once()
    {
        var accounts = Enumerable.Range(0, 20).Select(index => AccountIdFactory.Create(Guid.NewGuid())).ToList();

        _ = await Task.WhenAll(accounts.Select(account => Task.Run(() => services.Runner.RunAsync(account, Manual, CancellationToken), CancellationToken)));

        services.Quota.Calls.ShouldBe(accounts, ignoreOrder: true);
    }

    public void Dispose() => services.Dispose();
}
