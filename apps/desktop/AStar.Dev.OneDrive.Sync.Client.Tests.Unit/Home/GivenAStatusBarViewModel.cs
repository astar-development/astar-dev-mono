using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Onboarding;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenAStatusBarViewModel
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IGraphService _graphService = Substitute.For<IGraphService>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();

    private AccountsViewModel CreateAccountsViewModel() => new(_authService, _graphService, _accountRepository, Substitute.For<IAccountOnboardingService>(), Substitute.For<IQuotaRefreshService>(), _syncEventAggregator, _localizationService, Substitute.For<ILogger<AccountsViewModel>>());

    private AccountCardViewModel CreateCard(string email = "test@example.com", string displayName = "Test User") => new(new OneDriveAccount { Profile = AccountProfileFactory.Create(displayName, email) }, _localizationService);

    [Fact]
    public void when_active_account_is_null_then_has_account_is_false()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts, _localizationService);

        sut.HasAccount.ShouldBeFalse();
    }

    [Fact]
    public void when_active_account_is_set_then_email_matches_account_email()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts, _localizationService);
        var card = CreateCard(email: "jane@example.com");

        accounts.ActiveAccount = card;

        sut.AccountEmail.ShouldBe("jane@example.com");
    }

    [Fact]
    public void when_active_account_is_set_then_has_account_is_true()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts, _localizationService);
        var card = CreateCard();

        accounts.ActiveAccount = card;

        sut.HasAccount.ShouldBeTrue();
    }

    [Fact]
    public void when_active_account_sync_state_changes_then_status_bar_reflects_change()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts, _localizationService);
        var card = CreateCard();
        accounts.ActiveAccount = card;

        card.SyncState = SyncState.Syncing;

        sut.SyncState.ShouldBe(SyncState.Syncing);
    }

    [Fact]
    public void when_active_account_conflict_count_changes_then_status_bar_reflects_change()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts, _localizationService);
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
        var sut = new StatusBarViewModel(accounts, _localizationService);

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
        var sut = new StatusBarViewModel(accounts, _localizationService);

        accounts.ActiveAccount = secondCard;

        sut.AccountDisplayName.ShouldBe("Bob");
    }

    [Fact]
    public void when_sync_state_is_syncing_then_status_label_uses_syncing_key()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts, _localizationService);
        _localizationService.GetLocal(Arg.Any<string>()).Returns(key => key.ArgAt<string>(0));

        sut.SyncState = SyncState.Syncing;
        _ = sut.StatusLabel;

        _localizationService.Received(1).GetLocal("StatusBar.Syncing");
    }

    [Fact]
    public void when_sync_state_is_pending_then_status_label_uses_pending_key()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts, _localizationService);
        _localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(key => key.ArgAt<string>(0));

        sut.SyncState = SyncState.Pending;
        sut.PendingCount = 4;
        _ = sut.StatusLabel;

        _localizationService.Received(1).GetLocal("StatusBar.Pending", Arg.Any<object[]>());
    }

    [Fact]
    public void when_sync_state_is_conflict_with_one_conflict_then_status_label_uses_singular_conflict_key()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts, _localizationService);
        _localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(key => key.ArgAt<string>(0));

        sut.SyncState = SyncState.Conflict;
        sut.ConflictCount = 1;
        _ = sut.StatusLabel;

        _localizationService.Received(1).GetLocal("StatusBar.Conflict", Arg.Any<object[]>());
    }

    [Fact]
    public void when_sync_state_is_conflict_with_multiple_conflicts_then_status_label_uses_plural_conflicts_key()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts, _localizationService);
        _localizationService.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(key => key.ArgAt<string>(0));

        sut.SyncState = SyncState.Conflict;
        sut.ConflictCount = 3;
        _ = sut.StatusLabel;

        _localizationService.Received(1).GetLocal("StatusBar.Conflicts", Arg.Any<object[]>());
    }

    [Fact]
    public void when_sync_state_is_error_then_status_label_uses_error_key()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts, _localizationService);
        _localizationService.GetLocal(Arg.Any<string>()).Returns(key => key.ArgAt<string>(0));

        sut.SyncState = SyncState.Error;
        _ = sut.StatusLabel;

        _localizationService.Received(1).GetLocal("StatusBar.Error");
    }

    [Fact]
    public void when_sync_state_is_idle_then_status_label_uses_synced_key()
    {
        var accounts = CreateAccountsViewModel();
        var sut = new StatusBarViewModel(accounts, _localizationService);
        _localizationService.GetLocal(Arg.Any<string>()).Returns(key => key.ArgAt<string>(0));

        sut.SyncState = SyncState.Idle;
        _ = sut.StatusLabel;

        _localizationService.Received(1).GetLocal("StatusBar.Synced");
    }
}
