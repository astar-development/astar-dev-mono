using AStar.Dev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Rules;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Accounts;

public sealed class GivenAnAccountFilesViewModelCancellationTokenPropagation
{
    private const string AccountIdString = "account-1";
    private const string AccessToken = "token-abc";
    private const string DriveIdValue = "drive-1";

    [Fact]
    public async Task when_load_is_called_with_a_cancellation_token_then_get_rule_states_receives_it()
    {
        using var cts = new CancellationTokenSource();
        var capturedToken = CancellationToken.None;

        var syncRuleService = Substitute.For<ISyncRuleService>();
        syncRuleService.GetRuleStatesAsync(Arg.Any<AccountId>(), Arg.Do<CancellationToken>(t => capturedToken = t))
            .Returns(new Dictionary<string, RuleType>().AsReadOnly());

        var authService = Substitute.For<IAuthService>();
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdString, AccountProfileFactory.Create("Test User", "test@test.com")));

        var graphService = Substitute.For<IGraphService>();
        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Ok<DriveId, string>(new DriveId(DriveIdValue)));
        graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Ok<List<DriveFolder>, string>([]));
        var fileSystemServices = new FileSystemServices(Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>());

        var sut = new AccountFilesViewModel(BuildAccount(), authService, graphService, syncRuleService, fileSystemServices, Substitute.For<ILogger<AccountFilesViewModel>>(), Substitute.For<IFolderTreeNodeViewModelFactory>(), Substitute.For<ILocalizationService>());

        await sut.LoadAsync(cts.Token);

        capturedToken.ShouldBe(cts.Token);
    }

    private static OneDriveAccount BuildAccount()
        => new()
        {
            Id = new AccountId(AccountIdString),
            Profile = AccountProfileFactory.Create("Test User", "test@test.com"),
            SyncConfig = Option.Some(AccountSyncConfigFactory.Create(ConflictPolicy.LastWriteWins, LocalSyncPath.Restore("/tmp/sync")))
        };
}
