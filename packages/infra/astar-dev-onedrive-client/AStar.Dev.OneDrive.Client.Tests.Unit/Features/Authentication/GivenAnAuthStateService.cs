using AStar.Dev.OneDrive.Client.Features.Authentication;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Features.Authentication;

public sealed class GivenAnAuthStateService : IDisposable
{
    private readonly AuthStateService _authStateService = new();

    public void Dispose() => _authStateService.Dispose();

    [Fact]
    public void when_publish_auth_state_change_then_observable_emits_change()
    {
        var accountId = Guid.NewGuid();
        var changes = new List<(Guid, AccountAuthState)>();
        var subscription = _authStateService.AccountAuthStateChanged.Subscribe(change => changes.Add(change));

        _authStateService.PublishAuthStateChange(accountId, AccountAuthState.AuthRequired);

        changes.Count.ShouldBe(1);
        changes[0].Item1.ShouldBe(accountId);
        changes[0].Item2.ShouldBe(AccountAuthState.AuthRequired);

        subscription.Dispose();
    }

    [Fact]
    public void when_publish_multiple_changes_then_observable_emits_all()
    {
        var account1 = Guid.NewGuid();
        var account2 = Guid.NewGuid();
        var changes = new List<(Guid, AccountAuthState)>();
        var subscription = _authStateService.AccountAuthStateChanged.Subscribe(change => changes.Add(change));

        _authStateService.PublishAuthStateChange(account1, AccountAuthState.AuthRequired);
        _authStateService.PublishAuthStateChange(account2, AccountAuthState.Authenticated);

        changes.Count.ShouldBe(2);
        changes[0].Item1.ShouldBe(account1);
        changes[0].Item2.ShouldBe(AccountAuthState.AuthRequired);
        changes[1].Item1.ShouldBe(account2);
        changes[1].Item2.ShouldBe(AccountAuthState.Authenticated);

        subscription.Dispose();
    }

    [Fact]
    public void when_multiple_subscribers_then_both_receive_notifications()
    {
        var accountId = Guid.NewGuid();
        var changes1 = new List<(Guid, AccountAuthState)>();
        var changes2 = new List<(Guid, AccountAuthState)>();

        var sub1 = _authStateService.AccountAuthStateChanged.Subscribe(c => changes1.Add(c));
        var sub2 = _authStateService.AccountAuthStateChanged.Subscribe(c => changes2.Add(c));

        _authStateService.PublishAuthStateChange(accountId, AccountAuthState.AuthRequired);

        changes1.Count.ShouldBe(1);
        changes2.Count.ShouldBe(1);
        changes1[0].ShouldBe(changes2[0]);

        sub1.Dispose();
        sub2.Dispose();
    }
}
