using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountsViewModelWithACompletingWizard
{
    private const string AccessToken  = "token-123";
    private const string AccountIdStr = "account-1";
    private const string DisplayName  = "Test User";
    private const string Email        = "test@outlook.com";
    private const string FolderId1    = "f1";
    private const string FolderName1  = "Documents";
    private const string FolderId2    = "f2";
    private const string FolderName2  = "Desktop";

    [Fact]
    public async Task when_wizard_completes_with_selected_folders_then_sync_rules_are_written_to_sync_rule_repository()
    {
        var (authService, graphService, repository, syncRuleRepo) = BuildMocks();
        var sut = BuildSut(authService, graphService, repository, syncRuleRepo);

        await DriveWizardToCompletionAsync(sut, authService, graphService);

        await syncRuleRepo.Received().UpsertAsync(Arg.Any<AccountId>(), $"/{FolderName1}", RuleType.Include, FolderId1, Arg.Any<CancellationToken>());
        await syncRuleRepo.Received().UpsertAsync(Arg.Any<AccountId>(), $"/{FolderName2}", RuleType.Include, FolderId2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_wizard_completes_with_two_selected_folders_then_two_sync_rules_are_written()
    {
        var (authService, graphService, repository, syncRuleRepo) = BuildMocks();
        var sut = BuildSut(authService, graphService, repository, syncRuleRepo);

        await DriveWizardToCompletionAsync(sut, authService, graphService);

        await syncRuleRepo.Received(2).UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_wizard_completes_with_no_selected_folders_then_no_sync_rules_are_written()
    {
        var (authService, graphService, repository, syncRuleRepo) = BuildMocks();
        var sut = BuildSut(authService, graphService, repository, syncRuleRepo);

        await DriveWizardToCompletionWithNoFoldersAsync(sut, authService, graphService);

        await syncRuleRepo.DidNotReceive().UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_wizard_completes_then_account_is_persisted_to_account_repository()
    {
        var (authService, graphService, repository, syncRuleRepo) = BuildMocks();
        var sut = BuildSut(authService, graphService, repository, syncRuleRepo);

        await DriveWizardToCompletionAsync(sut, authService, graphService);

        await repository.Received(1).UpsertAsync(Arg.Any<AccountEntity>(), Arg.Any<CancellationToken>());
    }

    private static async Task DriveWizardToCompletionAsync(AccountsViewModel sut, IAuthService authService, IGraphService graphService)
    {
        sut.AddAccount();
        var wizard = sut.Wizard!;

        await wizard.OpenBrowserCommand.ExecuteAsync(null);
        await wizard.NextCommand.ExecuteAsync(null);
        await wizard.NextCommand.ExecuteAsync(null);
        await wizard.NextCommand.ExecuteAsync(null);

        await Task.Delay(200);
    }

    private static async Task DriveWizardToCompletionWithNoFoldersAsync(AccountsViewModel sut, IAuthService authService, IGraphService graphService)
    {
        sut.AddAccount();
        var wizard = sut.Wizard!;

        await wizard.OpenBrowserCommand.ExecuteAsync(null);
        await wizard.NextCommand.ExecuteAsync(null);
        wizard.SkipFoldersCommand.Execute(null);
        await wizard.NextCommand.ExecuteAsync(null);

        await Task.Delay(200);
    }

    private static (IAuthService AuthService, IGraphService GraphService, IAccountRepository Repository, ISyncRuleRepository SyncRuleRepo) BuildMocks()
    {
        var authService  = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();
        var repository   = Substitute.For<IAccountRepository>();
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();

        authService.SignInInteractiveAsync(Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdStr, AccountProfileFactory.Create(DisplayName, Email)));

        graphService.GetRootFoldersAsync(AccessToken, Arg.Any<CancellationToken>())
            .Returns([new DriveFolder(FolderId1, FolderName1), new DriveFolder(FolderId2, FolderName2)]);

        return (authService, graphService, repository, syncRuleRepo);
    }

    private static AccountsViewModel BuildSut(IAuthService authService, IGraphService graphService, IAccountRepository repository, ISyncRuleRepository syncRuleRepo)
        => new(authService, graphService, repository, syncRuleRepo, Substitute.For<ISyncEventAggregator>());
}
