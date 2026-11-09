using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAccountActionsReauthenticating : IDisposable
{
    private const string Personal = "00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad";

    private readonly AccountServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_same_account_signs_in_again_then_it_is_signed_in_with_its_id_unchanged()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        services.States.Record(accountId, AccountSignInStateFactory.CreateSignInRequired());

        var result = await services.Actions.ReauthenticateAsync(accountId, CancellationToken);

        result.Match(_ => (object)"signed-in", error => error).ShouldBe("signed-in");
        services.States.StateOf(accountId).ShouldBe(AccountSignInStateFactory.CreateSignedIn());
        (await services.Store.GetAllAsync(CancellationToken)).Match(accounts => accounts.Select(account => account.Id), _ => []).ShouldBe([accountId]);
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("DifferentAccount")]
    public async Task when_the_sign_in_is_cancelled_or_another_account_is_chosen_then_the_state_is_unchanged(string failureName)
    {
        var failure = failureName == "Cancelled" ? ReauthFailureFactory.CreateCancelled() : ReauthFailureFactory.CreateDifferentAccount();
        var accountId = await services.AddAsync(Personal, CancellationToken);
        services.States.Record(accountId, AccountSignInStateFactory.CreateSignInRequired());
        services.Reauthenticator.Reauthenticate = _ => Task.FromResult(Result.Failure<Unit, ReauthFailure>(failure));

        var result = await services.Actions.ReauthenticateAsync(accountId, CancellationToken);

        result.Match(_ => (object)"signed-in", error => error).ShouldBe(failure);
        services.States.StateOf(accountId).ShouldBe(AccountSignInStateFactory.CreateSignInRequired());
    }

    [Fact]
    public async Task when_a_pass_keeps_running_then_reauthentication_reports_sync_still_running_after_ten_seconds()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        using var running = ((SyncLockAttempt.Acquired)services.SyncLock.TryAcquire(accountId)).Lease;

        var result = await services.AdvanceUntilAsync(services.Actions.ReauthenticateAsync(accountId, CancellationToken));

        result.Match(_ => (object)"signed-in", error => error).ShouldBe(ReauthFailureFactory.CreateSyncStillRunning());
    }

    [Fact]
    public async Task when_the_account_is_removed_while_its_sign_in_window_is_open_then_the_removal_waits_and_reports_sync_still_running()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        var signInOpen = new TaskCompletionSource<Result<Unit, ReauthFailure>>(TaskCreationOptions.RunContinuationsAsynchronously);
        services.Reauthenticator.Reauthenticate = _ => signInOpen.Task;

        var reauthenticating = services.Actions.ReauthenticateAsync(accountId, CancellationToken);
        var removal = await services.AdvanceUntilAsync(services.Actions.RemoveAsync(accountId, CancellationToken));
        signInOpen.SetResult(Result.Success<Unit, ReauthFailure>(Unit.Instance));
        var reauth = await reauthenticating.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

        removal.Match(_ => (object)"removed", error => error).ShouldBe(AccountRemovalFailureFactory.CreateSyncStillRunning());
        reauth.Match(_ => (object)"signed-in", error => error).ShouldBe("signed-in");
        services.Steps.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_account_is_unknown_then_the_provider_reports_not_found()
    {
        var result = await services.Actions.ReauthenticateAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        result.Match(_ => (object)"signed-in", error => error).ShouldBe(ReauthFailureFactory.CreateProvider(ProviderErrorFactory.CreateNotFound()));
    }

    public void Dispose() => services.Dispose();
}
