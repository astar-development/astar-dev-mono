using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenAStatusBarViewModel
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IGraphService _graphService = Substitute.For<IGraphService>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();

    private AccountsViewModel CreateAccountsViewModel() => new(_authService, _graphService, _accountRepository, _syncEventAggregator);

    private static AccountCardViewModel CreateCard(string email = "test@example.com", string displayName = "Test User") => new(new OneDriveAccount { Email = email, DisplayName = displayName });

    [Fact]
    public void when_active_account_is_null_then_has_account_is_false()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts);

        sut.HasAccount.ShouldBeFalse();
    }

    [Fact]
    public void when_active_account_is_set_then_email_matches_account_email()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts);
        var card = CreateCard(email: "jane@example.com");

        accounts.ActiveAccount = card;

        sut.AccountEmail.ShouldBe("jane@example.com");
    }

    [Fact]
    public void when_active_account_is_set_then_has_account_is_true()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts);
        var card = CreateCard();

        accounts.ActiveAccount = card;

        sut.HasAccount.ShouldBeTrue();
    }

    [Fact]
    public void when_active_account_sync_state_changes_then_status_bar_reflects_change()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts);
        var card = CreateCard();
        accounts.ActiveAccount = card;

        card.SyncState = SyncState.Syncing;

        sut.SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public void when_active_account_conflict_count_changes_then_status_bar_reflects_change()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts);
        var card = CreateCard();
        accounts.ActiveAccount = card;

        card.ConflictCount = 3;

        sut.ConflictCount.ShouldBe(3);
    }

    [Fact]
    public void when_active_account_changes_to_null_then_has_account_becomes_false()
    {
        var accounts = CreateAccountsViewModel();
        var card = CreateCard();
        accounts.ActiveAccount = card;
        var sut = new StatusBarViewModel(accounts);

        accounts.ActiveAccount = null;

        sut.HasAccount.ShouldBeFalse();
    }

    [Fact]
    public void when_active_account_changes_then_display_name_matches_new_account()
    {
        var accounts = CreateAccountsViewModel();
        var firstCard = CreateCard(displayName: "Alice");
        var secondCard = CreateCard(displayName: "Bob", email: "bob@example.com");
        accounts.ActiveAccount = firstCard;
        var sut = new StatusBarViewModel(accounts);

        accounts.ActiveAccount = secondCard;

        sut.AccountDisplayName.ShouldBe("Bob");
    }
}
