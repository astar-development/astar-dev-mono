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

public sealed class GivenAnAccountFilesViewModelCancellationTokenPropagation
{
    private const string AccountIdString = "account-1";
    private const string AccessToken     = "token-abc";
    private const string DriveIdValue    = "drive-1";

    [Fact]
    public async Task when_load_is_called_with_a_cancellation_token_then_get_rule_states_receives_it()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken capturedToken = CancellationToken.None;

        var syncRuleService = Substitute.For<ISyncRuleService>();
        syncRuleService.GetRuleStatesAsync(Arg.Any<AccountId>(), Arg.Do<CancellationToken>(t => capturedToken = t))
            .Returns(new Dictionary<string, RuleType>().AsReadOnly());

        var authService = Substitute.For<IAuthService>();
        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdString, AccountProfileFactory.Create("Test User", "test@test.com")));

        var graphService = Substitute.For<IGraphService>();
        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DriveId, string>.Ok(new DriveId(DriveIdValue)));
        graphService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DriveFolder>, string>.Ok([]));

        var sut = new AccountFilesViewModel(BuildAccount(), authService, graphService, syncRuleService, Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>(), Substitute.For<ILogger<AccountFilesViewModel>>(), Substitute.For<IFolderTreeNodeViewModelFactory>(), Substitute.For<ILocalizationService>());

        await sut.LoadAsync(cts.Token);

        capturedToken.ShouldBe(cts.Token);
    }

    private static OneDriveAccount BuildAccount()
        => new()
        {
            Id      = new AccountId(AccountIdString),
            Profile = AccountProfileFactory.Create("Test User", "test@test.com"),
            SyncConfig = Option.Some(AccountSyncConfigFactory.Create(ConflictPolicy.LastWriteWins, LocalSyncPath.Restore("/tmp/sync")))
        };
}
