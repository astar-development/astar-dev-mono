using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountFilesViewModelFolderCountChanged
{
    private const string AccountIdString = "account-1";
    private const string AccessToken = "token-abc";
    private const string DriveIdValue = "drive-1";
    private const string FolderId = "folder-1";
    private const string FolderName = "Photos";

    [Fact]
    public async Task when_a_folder_is_toggled_included_then_folder_count_changed_event_is_raised_with_include_rule_count()
    {
        int? capturedCount = null;
        var callCount = 0;
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(callCount++ == 0
                ? new List<SyncRuleEntity>()
                : [new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = $"/{FolderName}", RuleType = RuleType.Include }]));

        var sut = BuildSut(syncRuleRepo);
        sut.FolderCountChanged += (_, count) => capturedCount = count;

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        capturedCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_folder_is_toggled_excluded_then_folder_count_changed_event_is_raised_with_zero_count()
    {
        int? capturedCount = null;
        var callCount = 0;
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();
        syncRuleRepo.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(callCount++ == 0
                ? [new SyncRuleEntity { AccountId = new AccountId(AccountIdString), RemotePath = $"/{FolderName}", RuleType = RuleType.Include }]
                : new List<SyncRuleEntity>()));

        var sut = BuildSut(syncRuleRepo);
        sut.FolderCountChanged += (_, count) => capturedCount = count;

        await sut.LoadCommand.ExecuteAsync(null);
        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        capturedCount.ShouldBe(0);
    }

    private static AccountFilesViewModel BuildSut(ISyncRuleRepository syncRuleRepo)
    {
        var authService = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();
        var repository = Substitute.For<IAccountRepository>();

        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdString, AccountProfileFactory.Create("Test User", "test@test.com")));

        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DriveId, string>.Ok(new DriveId(DriveIdValue)));

        graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(FolderId, FolderName, Option.None<string>())]));

        return new AccountFilesViewModel(BuildAccount(), authService, graphService, repository, new SyncRuleService(syncRuleRepo, Substitute.For<ILogger<SyncRuleService>>()), Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>(), Substitute.For<ILogger<AccountFilesViewModel>>(), new FolderTreeNodeViewModelFactory(graphService, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>()), Substitute.For<ILocalizationService>());
    }

    private static OneDriveAccount BuildAccount()
        => new()
        {
            Id = new AccountId(AccountIdString),
            Profile = AccountProfileFactory.Create("Test User", "test@test.com"),
            SyncConfig = Option.None<AccountSyncConfig>()
        };
}
