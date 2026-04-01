using AStar.Dev.OneDrive.Client.Features.Authentication;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Features.Authentication;

public sealed class GivenAnAuthStateService
{
    private readonly AuthStateService _authStateService = new();

    [Fact]
    public void WhenPublishAuthStateChange_ThenObservableEmitsChange()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var changes = new List<(Guid, AccountAuthState)>();
        var subscription = _authStateService.AccountAuthStateChanged.Subscribe(change => changes.Add(change));

        // Act
        _authStateService.PublishAuthStateChange(accountId, AccountAuthState.AuthRequired);

        // Assert
        changes.Count.ShouldBe(1);
        changes[0].Item1.ShouldBe(accountId);
        changes[0].Item2.ShouldBe(AccountAuthState.AuthRequired);

        subscription.Dispose();
    }

    [Fact]
    public void WhenPublishMultipleChanges_ThenObservableEmitsAll()
    {
        // Arrange
        var account1 = Guid.NewGuid();
        var account2 = Guid.NewGuid();
        var changes = new List<(Guid, AccountAuthState)>();
        var subscription = _authStateService.AccountAuthStateChanged.Subscribe(change => changes.Add(change));

        // Act
        _authStateService.PublishAuthStateChange(account1, AccountAuthState.AuthRequired);
        _authStateService.PublishAuthStateChange(account2, AccountAuthState.Authenticated);

        // Assert
        changes.Count.ShouldBe(2);
        changes[0].Item1.ShouldBe(account1);
        changes[0].Item2.ShouldBe(AccountAuthState.AuthRequired);
        changes[1].Item1.ShouldBe(account2);
        changes[1].Item2.ShouldBe(AccountAuthState.Authenticated);

        subscription.Dispose();
    }

    [Fact]
    public void WhenMultipleSubscribers_ThenBothReceiveNotifications()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var changes1 = new List<(Guid, AccountAuthState)>();
        var changes2 = new List<(Guid, AccountAuthState)>();

        var sub1 = _authStateService.AccountAuthStateChanged.Subscribe(c => changes1.Add(c));
        var sub2 = _authStateService.AccountAuthStateChanged.Subscribe(c => changes2.Add(c));

        // Act
        _authStateService.PublishAuthStateChange(accountId, AccountAuthState.AuthRequired);

        // Assert
        changes1.Count.ShouldBe(1);
        changes2.Count.ShouldBe(1);
        changes1[0].ShouldBe(changes2[0]);

        sub1.Dispose();
        sub2.Dispose();
    }
}
