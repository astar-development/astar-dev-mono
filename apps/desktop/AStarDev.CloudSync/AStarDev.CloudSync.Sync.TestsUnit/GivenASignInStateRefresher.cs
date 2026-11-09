using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenASignInStateRefresher : IDisposable
{
    private const string Personal = "00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad";
    private const string Work = "00000000-0000-0000-f1e2-d3c4b5a69788.9188040d-6c67-4c5b-b112-36a304b66dad";

    private readonly AccountServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_a_probe_says_sign_in_is_required_then_only_that_account_is_marked()
    {
        var expired = await services.AddAsync(Personal, CancellationToken);
        var healthy = await services.AddAsync(Work, CancellationToken);
        services.Probe.Probe = accountId => Option.Some(accountId == expired ? AccountSignInStateFactory.CreateSignInRequired() : AccountSignInStateFactory.CreateSignedIn());

        await services.Refresher.RefreshAllAsync(CancellationToken);

        (services.States.StateOf(expired), services.States.StateOf(healthy)).ShouldBe((AccountSignInStateFactory.CreateSignInRequired(), AccountSignInStateFactory.CreateSignedIn()));
    }

    [Fact]
    public async Task when_a_probe_cannot_tell_then_the_account_keeps_its_state()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        services.States.Record(accountId, AccountSignInStateFactory.CreateSignInRequired());
        services.Probe.Probe = _ => Option.None<AccountSignInState>();

        await services.Refresher.RefreshAllAsync(CancellationToken);

        services.States.StateOf(accountId).ShouldBe(AccountSignInStateFactory.CreateSignInRequired());
    }

    [Fact]
    public async Task when_the_accounts_cannot_be_read_then_nothing_changes_and_nothing_throws()
    {
        using var failing = new AccountServices(new TestHelpers.Accounts.FakeAccountStore { All = () => new ErrorResponse("Database locked.") });

        await failing.Refresher.RefreshAllAsync(CancellationToken);

        failing.States.StateOf(AccountIdFactory.Create(Guid.NewGuid())).ShouldBe(AccountSignInStateFactory.CreateSignedIn());
    }

    [Fact]
    public void when_an_account_was_never_checked_then_it_is_signed_in_and_forgetting_it_is_safe()
    {
        var accountId = AccountIdFactory.Create(Guid.NewGuid());

        services.States.Forget(accountId);

        services.States.StateOf(accountId).ShouldBe(AccountSignInStateFactory.CreateSignedIn());
    }

    [Fact]
    public async Task when_many_states_are_set_at_once_then_each_account_keeps_its_own()
    {
        var accounts = Enumerable.Range(0, 20).Select(_ => AccountIdFactory.Create(Guid.NewGuid())).ToList();

        await Task.WhenAll(accounts.Select((accountId, index) => Task.Run(() => services.States.Record(accountId, index % 2 == 0 ? AccountSignInStateFactory.CreateSignInRequired() : AccountSignInStateFactory.CreateSignedIn()), CancellationToken)));

        accounts.Select((accountId, index) => services.States.StateOf(accountId) is AccountSignInState.SignInRequired == (index % 2 == 0)).ShouldAllBe(matches => matches);
    }

    public void Dispose() => services.Dispose();
}
