using AStar.Dev.OneDriveSync.old.Models;
using AStar.Dev.OneDriveSync.old.Services;
using AStar.Dev.OneDriveSync.old.ViewModels;

namespace AStar.Dev.OneDriveSync.old.Tests.Unit.ViewModels;

[TestSubject(typeof(AccountsViewModel))]
public class AccountsViewModelShould
{
    private readonly IAccountStore _accountStore = Substitute.For<IAccountStore>();
    private readonly IMsalAuthService _authService = Substitute.For<IMsalAuthService>();
    private readonly IOneDriveFolderService _folderService = Substitute.For<IOneDriveFolderService>();
    private readonly AccountsViewModel _sut;

    public AccountsViewModelShould()
    {
        _accountStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountRecord>>([]));
        _sut = new AccountsViewModel(_accountStore, _authService, _folderService);
    }

    [Fact]
    public void ReportNoAccounts_WhenEmpty()
        => _sut.HasAccounts.ShouldBeFalse();

    [Fact]
    public async Task LoadPersistedAccounts()
    {
        _accountStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountRecord>>([new AccountRecord { AccountId = "a1", Email = "test@example.com", DisplayName = "Test User", LocalSyncPath = "/home/test/OneDrive/test" }]));

        await _sut.LoadAccountsAsync(TestContext.Current.CancellationToken);

        _sut.Accounts.Count.ShouldBe(1);
        _sut.HasAccounts.ShouldBeTrue();
        _sut.Accounts[0].Email.ShouldBe("test@example.com");
    }

    [Fact]
    public void ShowWizardWhenAddAccountCommandExecuted()
    {
        _sut.AddAccountCommand.Execute(null);

        _sut.IsWizardVisible.ShouldBeTrue();
        _sut.Wizard.ShouldNotBeNull();
    }

    [Fact]
    public async Task RejectOverlappingLocalSyncPaths()
    {
        _accountStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountRecord>>([new AccountRecord { AccountId = "a1", Email = "user1@example.com", LocalSyncPath = "/home/test/OneDrive/user1" }]));
        await _sut.LoadAccountsAsync(TestContext.Current.CancellationToken);

        var error = _sut.ValidateLocalSyncPath("/home/test/OneDrive/user1/subfolder");
        error.ShouldNotBeNull();
        error.ShouldContain("overlaps");
    }

    [Fact]
    public async Task AllowNonOverlappingLocalSyncPaths()
    {
        _accountStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountRecord>>([new AccountRecord { AccountId = "a1", Email = "user1@example.com", LocalSyncPath = "/home/test/OneDrive/user1" }]));
        await _sut.LoadAccountsAsync(TestContext.Current.CancellationToken);

        var error = _sut.ValidateLocalSyncPath("/home/test/OneDrive/user2");
        error.ShouldBeNull();
    }

    [Fact]
    public async Task RejectParentPathOverlap()
    {
        _accountStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountRecord>>([new AccountRecord { AccountId = "a1", Email = "user1@example.com", LocalSyncPath = "/home/test/OneDrive/user1/subfolder" }]));
        await _sut.LoadAccountsAsync(TestContext.Current.CancellationToken);

        var error = _sut.ValidateLocalSyncPath("/home/test/OneDrive/user1");
        error.ShouldNotBeNull();
        error.ShouldContain("overlaps");
    }
}
