using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountFilesViewModelToggleErrorSurfacing
{
    private const string AccountIdString     = "account-1";
    private const string LocalSyncPathString = "/configured/sync/path";
    private const string AccessToken         = "token-abc";
    private const string DriveIdValue        = "drive-1";
    private const string RootFolderId        = "folder-root";
    private const string RootFolderName      = "Photos";

    [Fact]
    public async Task when_apply_rule_throws_then_has_load_error_is_set()
    {
        var syncRuleService = Substitute.For<ISyncRuleService>();
        syncRuleService.GetRuleStatesAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<string, RuleType>>(new Dictionary<string, RuleType>()));
        syncRuleService.ApplyRuleAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<IReadOnlyList<(string RemotePath, string Id)>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new InvalidOperationException("service error")));

        var sut = BuildSut(BuildMocks(), syncRuleService);
        await sut.LoadCommand.ExecuteAsync(null);

        var errorSet = new TaskCompletionSource();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(sut.HasLoadError) && sut.HasLoadError)
                errorSet.TrySetResult();
        };

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        await errorSet.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        sut.HasLoadError.ShouldBeTrue();
    }

    [Fact]
    public async Task when_apply_rule_succeeds_then_has_load_error_remains_false()
    {
        var syncRuleService = Substitute.For<ISyncRuleService>();
        syncRuleService.GetRuleStatesAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<string, RuleType>>(new Dictionary<string, RuleType>()));
        syncRuleService.ApplyRuleAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<IReadOnlyList<(string RemotePath, string Id)>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var sut = BuildSut(BuildMocks(), syncRuleService);
        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders[0].ToggleIncludeCommand.Execute(null);

        sut.HasLoadError.ShouldBeFalse();
    }

    private static (IAuthService Auth, IGraphService Graph) BuildMocks()
    {
        var authService  = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();

        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdString, AccountProfileFactory.Create("Test User", "test@test.com")));

        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DriveId, string>.Ok(new DriveId(DriveIdValue)));

        graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([new DriveFolder(RootFolderId, RootFolderName, Option.None<string>())]));

        return (authService, graphService);
    }

    private static OneDriveAccount BuildAccount()
        => new()
        {
            Id         = new AccountId(AccountIdString),
            Profile    = AccountProfileFactory.Create("Test User", "test@test.com"),
            SyncConfig = Option.Some(AccountSyncConfigFactory.Create(ConflictPolicy.LastWriteWins, LocalSyncPath.Restore(LocalSyncPathString)))
        };

    private static AccountFilesViewModel BuildSut((IAuthService Auth, IGraphService Graph) mocks, ISyncRuleService syncRuleService)
        => new(BuildAccount(), mocks.Auth, mocks.Graph, syncRuleService, Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>(), Substitute.For<ILogger<AccountFilesViewModel>>(), new FolderTreeNodeViewModelFactory(mocks.Graph, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>()), Substitute.For<ILocalizationService>());
}
