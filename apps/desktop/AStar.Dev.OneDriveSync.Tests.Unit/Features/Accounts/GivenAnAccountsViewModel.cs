using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Features.Authentication;
using AStar.Dev.OneDrive.Client.Features.FolderBrowsing;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Accounts;

public sealed class GivenAnAccountsViewModel
{
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly IAuthStateService _authStateService = Substitute.For<IAuthStateService>();
    private readonly IRelativeTimeFormatter _timeFormatter = Substitute.For<IRelativeTimeFormatter>();
    private readonly Func<AddAccountWizardViewModel> _wizardFactory;
    private readonly IMsalClient _msalClient = Substitute.For<IMsalClient>();
    private readonly IOneDriveFolderService _folderService = Substitute.For<IOneDriveFolderService>();
    private readonly ILocalSyncPathService _pathService = Substitute.For<ILocalSyncPathService>();

    public GivenAnAccountsViewModel()
        => _wizardFactory = () => new AddAccountWizardViewModel(_msalClient, _folderService, _accountRepository, _pathService);

    [Fact]
    public async Task when_loaded_then_accounts_list_is_populated()
    {
        _accountRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([new Account { Id = Guid.NewGuid(), DisplayName = "Alice", Email = "a@b.com", MicrosoftAccountId = "ms-1" }]);
        _authStateService.AccountAuthStateChanged.Returns(System.Reactive.Linq.Observable.Empty<(Guid, AccountAuthState)>());
        _timeFormatter.Format(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>()).Returns("just now");

        var sut = CreateSut();
        await sut.LoadAsync(TestContext.Current.CancellationToken);

        sut.Accounts.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_loaded_with_no_accounts_then_accounts_list_is_empty()
    {
        _accountRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        _authStateService.AccountAuthStateChanged.Returns(System.Reactive.Linq.Observable.Empty<(Guid, AccountAuthState)>());

        var sut = CreateSut();
        await sut.LoadAsync(TestContext.Current.CancellationToken);

        sut.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_remove_is_requested_while_sync_is_active_then_removal_is_blocked()
    {
        var accountId = Guid.NewGuid();
        var account = new Account { Id = accountId, DisplayName = "Alice", Email = "a@b.com", MicrosoftAccountId = "ms-1", IsSyncActive = true };
        _accountRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([account]);
        _authStateService.AccountAuthStateChanged.Returns(System.Reactive.Linq.Observable.Empty<(Guid, AccountAuthState)>());

        var sut = CreateSut();
        await sut.LoadAsync(TestContext.Current.CancellationToken);

        bool canRemove = sut.Accounts.First().CanRemove;

        canRemove.ShouldBeFalse();
    }

    [Fact]
    public async Task when_remove_is_requested_while_sync_is_idle_then_removal_is_allowed()
    {
        var account = new Account { Id = Guid.NewGuid(), DisplayName = "Alice", Email = "a@b.com", MicrosoftAccountId = "ms-1", IsSyncActive = false };
        _accountRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([account]);
        _authStateService.AccountAuthStateChanged.Returns(System.Reactive.Linq.Observable.Empty<(Guid, AccountAuthState)>());

        var sut = CreateSut();
        await sut.LoadAsync(TestContext.Current.CancellationToken);

        bool canRemove = sut.Accounts.First().CanRemove;

        canRemove.ShouldBeTrue();
    }

    private AccountsViewModel CreateSut()
        => new(_accountRepository, _authStateService, _timeFormatter, _wizardFactory);
}
