using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Rules;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Accounts;

public sealed class GivenAnAccountFilesViewModelWithLocalOnlyFolders
{
    private const string AccountIdString = "account-1";
    private const string SyncRoot = "/configured/sync/path";
    private const string AccessToken = "token-abc";
    private const string DriveIdValue = "drive-1";

    private static (AccountFilesViewModel Sut, ISyncRuleRepository RuleRepository) BuildSut(MockFileSystem fileSystem, params string[] remoteRootNames)
    {
        var authService = Substitute.For<IAuthService>();
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdString, AccountProfileFactory.Create("Test User", "test@test.com")));

        var graphService = Substitute.For<IGraphService>();
        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Ok<DriveId, string>(new DriveId(DriveIdValue)));
        graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Ok<List<DriveFolder>, string>([.. remoteRootNames.Select(name => new DriveFolder($"remote-{name}", name, Option.None<string>()))]));

        var ruleRepository = Substitute.For<ISyncRuleRepository>();
        ruleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns([]);

        var account = new OneDriveAccount
        {
            Id = new AccountId(AccountIdString),
            Profile = AccountProfileFactory.Create("Test User", "test@test.com"),
            SyncConfig = Option.Some(AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore(SyncRoot)))
        };
        var services = new AccountFilesViewServices(authService, Substitute.For<ILocalizationService>(), graphService, new SyncRuleService(ruleRepository, Substitute.For<ILogger<SyncRuleService>>()));
        var fileSystemServices = new FileSystemServices(fileSystem, Substitute.For<IFileManagerService>());
        var factory = new FolderTreeNodeViewModelFactory(graphService, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>(), new LocalFolderLister(fileSystem));

        return (new AccountFilesViewModel(account, services, fileSystemServices, Substitute.For<ILogger<AccountFilesViewModel>>(), factory), ruleRepository);
    }

    [Fact]
    public async Task when_a_local_folder_exists_at_the_sync_root_then_it_is_listed_alongside_remote_folders()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory($"{SyncRoot}/Drafts");
        var (sut, _) = BuildSut(fileSystem, "Photos");

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders.Select(node => node.Name).ShouldBe(["Drafts", "Photos"]);
        sut.RootFolders.Single(node => node.Name == "Drafts").IsLocalOnly.ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_local_only_root_folder_has_no_rule_then_an_include_rule_without_a_remote_id_is_persisted()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory($"{SyncRoot}/Drafts");
        var (sut, ruleRepository) = BuildSut(fileSystem, "Photos");

        await sut.LoadCommand.ExecuteAsync(null);

        await ruleRepository.Received(1).UpsertAsync(Arg.Any<AccountId>(), "/Drafts", RuleType.Include, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_remote_root_folder_has_no_rule_then_no_rule_is_persisted_on_load()
    {
        var (sut, ruleRepository) = BuildSut(new MockFileSystem(), "Photos");

        await sut.LoadCommand.ExecuteAsync(null);

        await ruleRepository.DidNotReceive().UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_local_only_root_folder_is_excluded_then_an_exclude_rule_without_a_remote_id_is_persisted()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory($"{SyncRoot}/Drafts");
        var (sut, ruleRepository) = BuildSut(fileSystem);
        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders.Single().ToggleIncludeCommand.Execute(null);

        await ruleRepository.Received(1).UpsertAsync(Arg.Any<AccountId>(), "/Drafts", RuleType.Exclude, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_remote_root_folder_is_included_then_its_remote_id_is_still_persisted()
    {
        var (sut, ruleRepository) = BuildSut(new MockFileSystem(), "Photos");
        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders.Single().ToggleIncludeCommand.Execute(null);

        await ruleRepository.Received(1).UpsertAsync(Arg.Any<AccountId>(), "/Photos", RuleType.Include, "remote-Photos", Arg.Any<CancellationToken>());
    }
}
