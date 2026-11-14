using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenASyncRunJournal : IAsyncLifetime
{
    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId SecondAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset Start = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider time = new(Start);

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    private ISyncRunJournal Journal => Database.Journal;

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken, services => services.AddSingleton<TimeProvider>(time));

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    private async Task<SyncRunId> StartAsync(AccountId account) => (await Journal.StartAsync(account, SyncTriggerFactory.CreateScheduled(), CancellationToken)).Match(id => id, _ => throw new InvalidOperationException("The run must start."));

    private async Task FinishAsync(SyncRunId run, SyncRunOutcome outcome) => _ = await Journal.FinishAsync(run, outcome, CancellationToken);

    private async Task<IReadOnlyList<AccountId>> InterruptedAccountsAsync() => (await Journal.GetInterruptedAsync(CancellationToken)).Match(runs => (IReadOnlyList<AccountId>)[.. runs.Select(run => run.AccountId)], _ => throw new InvalidOperationException("The journal must be readable."));

    private async Task<Option<DateTimeOffset>> LastSyncAsync(AccountId account) => (await Journal.GetLastSyncAsync(account, CancellationToken)).Match(last => last, _ => throw new InvalidOperationException("The journal must be readable."));

    private async Task<long> RowsAsync(AccountId account)
    {
        await using var connection = new SqliteConnection($"Data Source={Database.DatabasePath}");
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM SyncRuns WHERE upper(AccountId) = $account";
        _ = command.Parameters.AddWithValue("$account", account.Value.ToString().ToUpperInvariant());

        return (long)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    [Fact]
    public async Task when_a_run_starts_and_never_finishes_then_it_is_found_as_interrupted_with_its_account()
    {
        _ = await StartAsync(FirstAccount);

        (await InterruptedAccountsAsync()).ShouldBe([FirstAccount]);
    }

    [Fact]
    public async Task when_runs_finish_with_any_outcome_then_none_is_interrupted()
    {
        var outcomes = new[] { SyncRunOutcomeFactory.CreateCompleted(), SyncRunOutcomeFactory.CreateCancelled(), SyncRunOutcomeFactory.CreateFailed(SyncFailureReasonFactory.CreateQuotaExceeded()), SyncRunOutcomeFactory.CreateInterrupted() };
        foreach (var outcome in outcomes)
            await FinishAsync(await StartAsync(FirstAccount), outcome);

        (await InterruptedAccountsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_runs_are_interrupted_on_two_accounts_then_both_are_found()
    {
        _ = await StartAsync(FirstAccount);
        _ = await StartAsync(SecondAccount);

        (await InterruptedAccountsAsync()).ShouldBe([FirstAccount, SecondAccount], ignoreOrder: true);
    }

    [Fact]
    public async Task when_an_account_never_synced_then_it_has_no_last_sync()
    {
        (await LastSyncAsync(FirstAccount)).ShouldBe(Option.None<DateTimeOffset>());
    }

    [Fact]
    public async Task when_runs_completed_then_the_last_sync_is_the_newest_finish_of_that_account_only()
    {
        var first = await StartAsync(FirstAccount);
        time.Advance(TimeSpan.FromMinutes(1));
        await FinishAsync(first, SyncRunOutcomeFactory.CreateCompleted());
        var firstFinished = time.GetUtcNow();
        time.Advance(TimeSpan.FromMinutes(5));
        var second = await StartAsync(FirstAccount);
        time.Advance(TimeSpan.FromMinutes(1));
        await FinishAsync(second, SyncRunOutcomeFactory.CreateCompleted());
        var secondFinished = time.GetUtcNow();
        var other = await StartAsync(SecondAccount);
        time.Advance(TimeSpan.FromMinutes(1));
        await FinishAsync(other, SyncRunOutcomeFactory.CreateCompleted());

        (await LastSyncAsync(FirstAccount)).ShouldBe(Option.Some(secondFinished));
        secondFinished.ShouldBeGreaterThan(firstFinished);
    }

    [Fact]
    public async Task when_the_newest_runs_failed_or_were_cancelled_then_the_last_sync_stays_the_last_completed_run()
    {
        var done = await StartAsync(FirstAccount);
        await FinishAsync(done, SyncRunOutcomeFactory.CreateCompleted());
        var completedAt = time.GetUtcNow();
        time.Advance(TimeSpan.FromMinutes(10));
        await FinishAsync(await StartAsync(FirstAccount), SyncRunOutcomeFactory.CreateFailed(SyncFailureReasonFactory.CreateUnexpected()));
        await FinishAsync(await StartAsync(FirstAccount), SyncRunOutcomeFactory.CreateCancelled());
        await FinishAsync(await StartAsync(FirstAccount), SyncRunOutcomeFactory.CreateInterrupted());

        (await LastSyncAsync(FirstAccount)).ShouldBe(Option.Some(completedAt));
    }

    [Fact]
    public async Task when_a_finished_run_is_finished_again_then_the_first_outcome_stands()
    {
        var run = await StartAsync(FirstAccount);
        await FinishAsync(run, SyncRunOutcomeFactory.CreateCompleted());
        var completedAt = time.GetUtcNow();
        time.Advance(TimeSpan.FromMinutes(3));

        await FinishAsync(run, SyncRunOutcomeFactory.CreateFailed(SyncFailureReasonFactory.CreateUnexpected()));

        (await LastSyncAsync(FirstAccount)).ShouldBe(Option.Some(completedAt));
    }

    [Fact]
    public async Task when_an_unknown_run_is_finished_then_nothing_changes_and_no_error_is_returned()
    {
        var outcome = await Journal.FinishAsync(new SyncRunId(Guid.NewGuid()), SyncRunOutcomeFactory.CreateCompleted(), CancellationToken);

        outcome.Match(_ => "ok", error => error.Message).ShouldBe("ok");
        (await InterruptedAccountsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_more_than_the_history_limit_finish_then_only_the_newest_runs_of_that_account_are_kept()
    {
        foreach (var _ in Enumerable.Range(0, SyncRunJournalLimits.RunsPerAccount + 5))
        {
            var run = await StartAsync(FirstAccount);
            time.Advance(TimeSpan.FromSeconds(1));
            await FinishAsync(run, SyncRunOutcomeFactory.CreateCompleted());
            time.Advance(TimeSpan.FromSeconds(1));
        }

        var other = await StartAsync(SecondAccount);
        await FinishAsync(other, SyncRunOutcomeFactory.CreateCompleted());

        (await RowsAsync(FirstAccount), await RowsAsync(SecondAccount)).ShouldBe((SyncRunJournalLimits.RunsPerAccount, 1L));
    }

    [Fact]
    public async Task when_the_history_is_pruned_then_a_run_still_in_progress_is_kept_and_the_last_sync_is_the_newest_completed_run()
    {
        _ = await StartAsync(FirstAccount);
        time.Advance(TimeSpan.FromSeconds(1));
        foreach (var _ in Enumerable.Range(0, SyncRunJournalLimits.RunsPerAccount + 2))
        {
            var run = await StartAsync(FirstAccount);
            time.Advance(TimeSpan.FromSeconds(1));
            await FinishAsync(run, SyncRunOutcomeFactory.CreateCompleted());
            time.Advance(TimeSpan.FromSeconds(1));
        }

        (await InterruptedAccountsAsync()).ShouldBe([FirstAccount]);
        (await LastSyncAsync(FirstAccount)).ShouldBe(Option.Some(time.GetUtcNow().AddSeconds(-1)));
    }

    [Fact]
    public async Task when_more_than_the_history_limit_of_runs_failed_since_the_last_completed_run_then_the_last_sync_is_still_known()
    {
        await FinishAsync(await StartAsync(FirstAccount), SyncRunOutcomeFactory.CreateCompleted());
        var completedAt = time.GetUtcNow();
        foreach (var _ in Enumerable.Range(0, SyncRunJournalLimits.RunsPerAccount + 5))
        {
            time.Advance(TimeSpan.FromSeconds(1));
            await FinishAsync(await StartAsync(FirstAccount), SyncRunOutcomeFactory.CreateFailed(SyncFailureReasonFactory.CreateQuotaExceeded()));
        }

        (await LastSyncAsync(FirstAccount)).ShouldBe(Option.Some(completedAt));
    }

    [Fact]
    public async Task when_twenty_runs_start_and_finish_at_once_then_every_one_is_finished_and_none_is_lost()
    {
        var work = Enumerable.Range(0, 20).Select(_ => Task.Run(async () => await FinishAsync(await StartAsync(FirstAccount), SyncRunOutcomeFactory.CreateCompleted()), CancellationToken));

        await Task.WhenAll(work);

        (await InterruptedAccountsAsync()).ShouldBeEmpty();
        (await RowsAsync(FirstAccount)).ShouldBe(20L);
    }

    [Fact]
    public async Task when_an_account_is_removed_then_its_runs_go_and_other_accounts_keep_theirs()
    {
        _ = await StartAsync(FirstAccount);
        _ = await StartAsync(SecondAccount);

        _ = await Database.JournalParticipant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        (await InterruptedAccountsAsync()).ShouldBe([SecondAccount]);
    }

    [Fact]
    public async Task when_the_database_cannot_be_read_then_the_journal_returns_an_error()
    {
        await using var broken = TemporarySyncDatabase.CreateUnmigrated();

        var outcome = await broken.Journal.GetInterruptedAsync(CancellationToken);

        outcome.Match(_ => "ok", error => error.Message).ShouldNotBe("ok");
    }
}
