using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using AStar.Dev.Functional.Extensions;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

public sealed class GivenAnAccountFilesViewModelWithDriveIdFetchFailure
{
    private const string AccountIdString = "account-1";
    private const string AccessToken = "token-abc";
    private const string DriveIdErrorMessage = "Failed to retrieve drive: 403 Forbidden";

    [Fact]
    public async Task when_get_drive_id_fails_then_has_load_error_is_true()
    {
        var sut = BuildSut();

        await sut.LoadCommand.ExecuteAsync(null);

        sut.HasLoadError.ShouldBeTrue();
    }

    [Fact]
    public async Task when_get_drive_id_fails_then_load_error_contains_failure_message()
    {
        var sut = BuildSut();

        await sut.LoadCommand.ExecuteAsync(null);

        sut.LoadError.ShouldBe($"Failed to retrieve drive ID: {DriveIdErrorMessage}");
    }

    [Fact]
    public async Task when_get_drive_id_fails_then_root_folders_remain_empty()
    {
        var sut = BuildSut();

        await sut.LoadCommand.ExecuteAsync(null);

        sut.RootFolders.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_get_drive_id_fails_then_is_loading_is_false()
    {
        var sut = BuildSut();

        await sut.LoadCommand.ExecuteAsync(null);

        sut.IsLoading.ShouldBeFalse();
    }

    private static AccountFilesViewModel BuildSut()
    {
        var authService = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();
        var syncRuleRepo = Substitute.For<ISyncRuleRepository>();

        authService.AcquireTokenSilentAsync(AccountIdString, Arg.Any<CancellationToken>())
            .Returns(AuthResultFactory.Success(AccessToken, AccountIdString, AccountProfileFactory.Create("Test User", "test@test.com")));

        graphService.GetDriveIdAsync(AccountIdString, Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DriveId, string>.Error(DriveIdErrorMessage));

        return new AccountFilesViewModel(BuildAccount(), authService, graphService, new SyncRuleService(syncRuleRepo, Substitute.For<ILogger<SyncRuleService>>()), Substitute.For<IFileSystem>(), Substitute.For<IFileManagerService>(), Substitute.For<ILogger<AccountFilesViewModel>>(), new FolderTreeNodeViewModelFactory(graphService, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>()), Substitute.For<ILocalizationService>());
    }

    private static OneDriveAccount BuildAccount() => new()
    {
        Id      = new AccountId(AccountIdString),
        Profile = AccountProfileFactory.Create("Test User", "test@test.com")
    };
}
