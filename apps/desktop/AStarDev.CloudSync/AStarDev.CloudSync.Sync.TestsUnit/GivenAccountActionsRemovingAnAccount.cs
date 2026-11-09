using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAccountActionsRemovingAnAccount : IDisposable
{
    private const string Personal = "00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad";
    private const string Work = "00000000-0000-0000-f1e2-d3c4b5a69788.9188040d-6c67-4c5b-b112-36a304b66dad";

    private readonly AccountServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_an_account_is_removed_then_it_signs_out_then_clears_every_module_then_deletes_the_account()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        var otherId = await services.AddAsync(Work, CancellationToken);

        var result = await services.Actions.RemoveAsync(accountId, CancellationToken);

        result.Match(_ => (object)"removed", error => error).ShouldBe("removed");
        services.Steps.ShouldBe(["sign-out", "token-cache", "sync-data"]);
        (await services.Store.GetAllAsync(CancellationToken)).Match(accounts => accounts.Select(account => account.Id), _ => []).ShouldBe([otherId]);
    }

    [Fact]
    public async Task when_the_account_is_unknown_then_the_removal_is_not_found_and_nothing_runs()
    {
        var result = await services.Actions.RemoveAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        result.Match(_ => (object)"removed", error => error).ShouldBe(AccountRemovalFailureFactory.CreateNotFound());
        services.Steps.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_account_is_removed_twice_then_the_second_removal_is_not_found()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        _ = await services.Actions.RemoveAsync(accountId, CancellationToken);

        var second = await services.Actions.RemoveAsync(accountId, CancellationToken);

        second.Match(_ => (object)"removed", error => error).ShouldBe(AccountRemovalFailureFactory.CreateNotFound());
    }

    [Fact]
    public async Task when_a_pass_keeps_running_then_removal_waits_ten_seconds_then_reports_sync_still_running_and_deletes_nothing()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        using var running = ((SyncLockAttempt.Acquired)services.SyncLock.TryAcquire(accountId)).Lease;

        var removing = services.Actions.RemoveAsync(accountId, CancellationToken);
        var result = await services.AdvanceUntilAsync(removing);

        result.Match(_ => (object)"removed", error => error).ShouldBe(AccountRemovalFailureFactory.CreateSyncStillRunning());
        services.Steps.ShouldBeEmpty();
        (await services.Store.GetAllAsync(CancellationToken)).Match(accounts => accounts.Count, _ => -1).ShouldBe(1);
    }

    [Fact]
    public async Task when_a_pass_ends_within_ten_seconds_then_the_removal_goes_ahead()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        var running = ((SyncLockAttempt.Acquired)services.SyncLock.TryAcquire(accountId)).Lease;

        var removing = services.Actions.RemoveAsync(accountId, CancellationToken);
        services.Time.Advance(TimeSpan.FromSeconds(3));
        running.Dispose();
        var result = await services.AdvanceUntilAsync(removing);

        result.Match(_ => (object)"removed", error => error).ShouldBe("removed");
    }

    [Fact]
    public async Task when_the_provider_cannot_sign_out_then_the_removal_stops_and_nothing_is_deleted()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        services.SignOut.Outcome = Result.Failure<Unit, ProviderError>(ProviderErrorFactory.CreateTransient());

        var result = await services.Actions.RemoveAsync(accountId, CancellationToken);

        result.Match(_ => (object)"removed", error => error).ShouldBe(AccountRemovalFailureFactory.CreateProvider(ProviderErrorFactory.CreateTransient()));
        services.Steps.ShouldBe(["sign-out"]);
        (await services.Store.GetAllAsync(CancellationToken)).Match(accounts => accounts.Count, _ => -1).ShouldBe(1);
    }

    [Fact]
    public async Task when_a_module_cannot_delete_its_data_then_the_removal_stops_before_the_account_is_deleted()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        services.Cache.Outcome = Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The cache is locked."));

        var result = await services.Actions.RemoveAsync(accountId, CancellationToken);

        result.Match(_ => (object)"removed", error => error).ShouldBeOfType<AccountRemovalFailure.Store>();
        services.Steps.ShouldBe(["sign-out", "token-cache"]);
        (await services.Store.GetAllAsync(CancellationToken)).Match(accounts => accounts.Count, _ => -1).ShouldBe(1);
    }

    [Fact]
    public async Task when_a_step_after_sign_out_fails_then_the_account_is_marked_as_needing_sign_in()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        services.Sync.Outcome = Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The sync tables are locked."));

        _ = await services.Actions.RemoveAsync(accountId, CancellationToken);

        services.States.StateOf(accountId).ShouldBe(AccountSignInStateFactory.CreateSignInRequired());
    }

    [Fact]
    public async Task when_sign_out_fails_then_the_sign_in_state_is_unchanged()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        services.SignOut.Outcome = Result.Failure<Unit, ProviderError>(ProviderErrorFactory.CreateTransient());

        _ = await services.Actions.RemoveAsync(accountId, CancellationToken);

        services.States.StateOf(accountId).ShouldBe(AccountSignInStateFactory.CreateSignedIn());
    }

    [Fact]
    public async Task when_the_lock_is_drained_then_removal_reports_sync_still_running_without_waiting()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        using var drain = await services.SyncLock.DrainAsync(CancellationToken);

        var removing = services.Actions.RemoveAsync(accountId, CancellationToken);
        var result = await removing.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        result.Match(_ => (object)"removed", error => error).ShouldBe(AccountRemovalFailureFactory.CreateSyncStillRunning());
    }

    [Fact]
    public async Task when_the_accounts_cannot_be_read_then_the_removal_is_a_store_failure()
    {
        using var failing = new AccountServices(new TestHelpers.Accounts.FakeAccountStore { All = () => new ErrorResponse("Database locked.") });

        var result = await failing.Actions.RemoveAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        result.Match(_ => (object)"removed", error => error).ShouldBeOfType<AccountRemovalFailure.Store>();
    }

    [Fact]
    public async Task when_the_account_cannot_be_deleted_then_the_removal_is_a_store_failure()
    {
        var accountId = TestHelpers.Accounts.FakeAccountStore.AddedAccountId;
        var stored = StoredAccountFactory.Create(accountId, AccountFixtures.Registration(Personal));
        using var withAccount = new AccountServices(new TestHelpers.Accounts.FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([stored]), Remove = _ => new ErrorResponse("Database locked.") });

        var result = await withAccount.Actions.RemoveAsync(accountId, CancellationToken);

        result.Match(_ => (object)"removed", error => error).ShouldBeOfType<AccountRemovalFailure.Store>();
        withAccount.Steps.ShouldBe(["sign-out", "token-cache", "sync-data"]);
    }

    [Fact]
    public async Task when_a_removed_account_had_a_sign_in_state_then_it_is_forgotten()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        services.States.Record(accountId, AccountSignInStateFactory.CreateSignInRequired());

        _ = await services.Actions.RemoveAsync(accountId, CancellationToken);

        services.States.StateOf(accountId).ShouldBe(AccountSignInStateFactory.CreateSignedIn());
    }

    [Fact]
    public async Task when_one_account_is_removed_many_times_at_once_then_exactly_one_removal_succeeds()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);

        var results = await services.AdvanceUntilAsync(Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => services.Actions.RemoveAsync(accountId, CancellationToken), CancellationToken))));
        var settled = results.Count(result => result.Match(_ => true, _ => false));

        settled.ShouldBe(1);
        services.Steps.Count(step => step == "sign-out").ShouldBe(1);
    }

    [Fact]
    public async Task when_a_removal_stops_then_the_warning_names_no_email_or_name()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        services.SignOut.Outcome = Result.Failure<Unit, ProviderError>(ProviderErrorFactory.CreateTransient());

        _ = await services.Actions.RemoveAsync(accountId, CancellationToken);

        services.Logs.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        services.Logs.Logger.Messages.ShouldAllBe(message => !message.Contains(AccountFixtures.Email, StringComparison.Ordinal) && !message.Contains("Jane Doe", StringComparison.Ordinal));
    }

    public void Dispose() => services.Dispose();
}
