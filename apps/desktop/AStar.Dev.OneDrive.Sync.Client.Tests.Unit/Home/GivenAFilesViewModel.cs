using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenAFilesViewModel
{
    private const string FirstAccountId = "account-1";
    private const string SecondAccountId = "account-2";
    private const string AccessToken = "token-abc";
    private const string DriveIdValue = "drive-1";
    private const string FolderId = "folder-1";
    private const string FolderName = "Photos";

    private static FilesViewModel CreateSut(IAccountFilesViewModelFactory? factory = null)
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());

        return new FilesViewModel(factory ?? CreateFactory(), localization);
    }

    private static IAccountFilesViewModelFactory CreateFactory(ISyncRuleRepository? syncRuleRepository = null)
    {
        var factory = Substitute.For<IAccountFilesViewModelFactory>();
        factory.Create(Arg.Any<OneDriveAccount>()).Returns(call => BuildTab(call.Arg<OneDriveAccount>(), syncRuleRepository ?? CreateEmptySyncRuleRepository()));

        return factory;
    }

    private static ISyncRuleRepository CreateEmptySyncRuleRepository()
    {
        var syncRuleRepository = Substitute.For<ISyncRuleRepository>();
        syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<SyncRuleEntity>()));

        return syncRuleRepository;
    }

    private static AccountFilesViewModel BuildTab(OneDriveAccount account, ISyncRuleRepository syncRuleRepository)
    {
        var authService = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();

        authService.AcquireTokenSilentAsync(account.Id.Id, Arg.Any<CancellationToken>()).Returns(AuthResultFactory.Success(AccessToken, account.Id.Id, account.Profile));
        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Result<DriveId, string>.Ok(new DriveId(DriveIdValue)));
        graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(FolderId, FolderName, Option.None<string>())]));

        return new AccountFilesViewModel(account, authService, graphService, new SyncRuleService(syncRuleRepository, Substitute.For<ILogger<SyncRuleService>>()), Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>(), Substitute.For<ILogger<AccountFilesViewModel>>(), new FolderTreeNodeViewModelFactory(graphService, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>()), Substitute.For<ILocalizationService>());
    }

    private static OneDriveAccount BuildAccount(string accountId)
        => new()
        {
            Id = new AccountId(accountId),
            Profile = AccountProfileFactory.Create("Test User", "test@test.com"),
            SyncConfig = Option.None<AccountSyncConfig>()
        };

    [Fact]
    public void when_no_accounts_have_been_added_then_view_model_reports_no_tabs()
    {
        var sut = CreateSut();

        sut.Tabs.ShouldBeEmpty();
        sut.HasTabs.ShouldBeFalse();
        sut.HasNoAccounts.ShouldBeTrue();
        sut.ActiveTab.ShouldBeNull();
    }

    [Fact]
    public void when_first_account_is_added_then_tab_is_created_and_becomes_active()
    {
        var sut = CreateSut();

        sut.AddAccount(BuildAccount(FirstAccountId));

        sut.Tabs.Count.ShouldBe(1);
        sut.HasTabs.ShouldBeTrue();
        sut.HasNoAccounts.ShouldBeFalse();
        sut.ActiveTab.ShouldBe(sut.Tabs[0]);
        sut.Tabs[0].IsActiveTab.ShouldBeTrue();
    }

    [Fact]
    public void when_same_account_is_added_twice_then_duplicate_tab_is_not_created()
    {
        var factory = CreateFactory();
        var sut = CreateSut(factory);

        sut.AddAccount(BuildAccount(FirstAccountId));
        sut.AddAccount(BuildAccount(FirstAccountId));

        sut.Tabs.Count.ShouldBe(1);
        factory.Received(1).Create(Arg.Any<OneDriveAccount>());
    }

    [Fact]
    public void when_second_account_is_added_then_first_tab_remains_active()
    {
        var sut = CreateSut();

        sut.AddAccount(BuildAccount(FirstAccountId));
        sut.AddAccount(BuildAccount(SecondAccountId));

        sut.Tabs.Count.ShouldBe(2);
        sut.ActiveTab!.AccountId.ShouldBe(FirstAccountId);
        sut.Tabs[1].IsActiveTab.ShouldBeFalse();
    }

    [Fact]
    public void when_active_account_is_removed_then_first_remaining_tab_becomes_active()
    {
        var sut = CreateSut();
        sut.AddAccount(BuildAccount(FirstAccountId));
        sut.AddAccount(BuildAccount(SecondAccountId));

        sut.RemoveAccount(FirstAccountId);

        sut.Tabs.Count.ShouldBe(1);
        sut.ActiveTab!.AccountId.ShouldBe(SecondAccountId);
        sut.Tabs[0].IsActiveTab.ShouldBeTrue();
    }

    [Fact]
    public void when_inactive_account_is_removed_then_active_tab_is_unchanged()
    {
        var sut = CreateSut();
        sut.AddAccount(BuildAccount(FirstAccountId));
        sut.AddAccount(BuildAccount(SecondAccountId));

        sut.RemoveAccount(SecondAccountId);

        sut.Tabs.Count.ShouldBe(1);
        sut.ActiveTab!.AccountId.ShouldBe(FirstAccountId);
    }

    [Fact]
    public void when_last_account_is_removed_then_no_tab_is_active()
    {
        var sut = CreateSut();
        sut.AddAccount(BuildAccount(FirstAccountId));

        sut.RemoveAccount(FirstAccountId);

        sut.Tabs.ShouldBeEmpty();
        sut.HasNoAccounts.ShouldBeTrue();
        sut.ActiveTab.ShouldBeNull();
    }

    [Fact]
    public void when_unknown_account_is_removed_then_tabs_are_unchanged()
    {
        var sut = CreateSut();
        sut.AddAccount(BuildAccount(FirstAccountId));

        sut.RemoveAccount("missing-account");

        sut.Tabs.Count.ShouldBe(1);
        sut.ActiveTab!.AccountId.ShouldBe(FirstAccountId);
    }

    [Fact]
    public async Task when_account_is_activated_then_tab_loads_and_becomes_active()
    {
        var sut = CreateSut();
        sut.AddAccount(BuildAccount(FirstAccountId));
        sut.AddAccount(BuildAccount(SecondAccountId));

        await sut.ActivateAccountAsync(SecondAccountId);

        sut.ActiveTab!.AccountId.ShouldBe(SecondAccountId);
        sut.Tabs[0].IsActiveTab.ShouldBeFalse();
        sut.ActiveTab.RootFolders.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_unknown_account_is_activated_then_active_tab_is_unchanged()
    {
        var sut = CreateSut();
        sut.AddAccount(BuildAccount(FirstAccountId));

        await sut.ActivateAccountAsync("missing-account");

        sut.ActiveTab!.AccountId.ShouldBe(FirstAccountId);
    }

    [Fact]
    public async Task when_a_tab_raises_folder_count_changed_then_event_is_forwarded_with_account_id()
    {
        int callCount = 0;
        var syncRuleRepository = Substitute.For<ISyncRuleRepository>();
        syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(callCount++ == 0 ? new List<SyncRuleEntity>() : [new SyncRuleEntity { AccountId = new AccountId(FirstAccountId), RemotePath = $"/{FolderName}", RuleType = RuleType.Include }]));
        var sut = CreateSut(CreateFactory(syncRuleRepository));
        sut.AddAccount(BuildAccount(FirstAccountId));
        await sut.ActivateAccountAsync(FirstAccountId);
        (string AccountId, int FolderCount)? captured = null;
        sut.FolderCountChanged += (_, payload) => captured = payload;

        sut.ActiveTab!.RootFolders[0].ToggleIncludeCommand.Execute(null);

        captured.ShouldNotBeNull();
        captured.Value.AccountId.ShouldBe(FirstAccountId);
        captured.Value.FolderCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_tab_raises_view_activity_then_event_is_forwarded_with_account_and_folder_id()
    {
        var sut = CreateSut();
        sut.AddAccount(BuildAccount(FirstAccountId));
        await sut.ActivateAccountAsync(FirstAccountId);
        (string AccountId, string FolderId)? captured = null;
        sut.ViewActivityRequested += (_, payload) => captured = payload;

        sut.ActiveTab!.RootFolders[0].ViewActivityCommand.Execute(null);

        captured.ShouldNotBeNull();
        captured.Value.AccountId.ShouldBe(FirstAccountId);
        captured.Value.FolderId.ShouldBe(FolderId);
    }

    [Fact]
    public void when_localised_texts_are_requested_then_localization_keys_are_used()
    {
        var sut = CreateSut();

        sut.NoAccountsConnectedText.ShouldBe("Files.NoAccountsConnected");
        sut.NoAccountsConnectedHintText.ShouldBe("Files.NoAccountsConnectedHint");
        sut.LoadingFoldersText.ShouldBe("Files.LoadingFolders");
        sut.CouldNotLoadText.ShouldBe("Files.CouldNotLoad");
    }
}
