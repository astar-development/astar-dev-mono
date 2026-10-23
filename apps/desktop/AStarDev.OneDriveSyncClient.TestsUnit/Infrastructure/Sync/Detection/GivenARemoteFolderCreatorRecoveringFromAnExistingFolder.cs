using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Detection;

public sealed class GivenARemoteFolderCreatorRecoveringFromAnExistingFolder
{
    private const string SyncRoot = "/sync-root";
    private static readonly DriveId Drive = new("drive-1");

    private readonly IGraphService graphService = Substitute.For<IGraphService>();
    private readonly ISyncRuleRepository syncRuleRepository = Substitute.For<ISyncRuleRepository>();
    private readonly MockFileSystem fileSystem = new();

    public GivenARemoteFolderCreatorRecoveringFromAnExistingFolder()
    {
        fileSystem.Directory.CreateDirectory("/sync-root/Drafts");
        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Ok<DriveId, string>(Drive));
        graphService.CreateFolderAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Fail<DriveFolder, string>("Name already exists"));
    }

    private Task Run()
    {
        var account = new OneDriveAccount
        {
            Id = new AccountId("user-1"),
            Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
            SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore(SyncRoot)),
            SelectedFolderIds = []
        };
        var rule = new SyncRuleEntity { RemotePath = "/Drafts", RuleType = RuleType.Include, RemoteItemId = Option.None<string>() };
        var sut = new RemoteFolderCreator(graphService, syncRuleRepository, fileSystem, Substitute.For<ILogger<RemoteFolderCreator>>());

        return sut.CreateMissingFoldersAsync(account, AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore(SyncRoot)), _ => Task.FromResult("token"), new RemoteEnumerationContext { Rules = [rule] }, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task when_create_reports_the_folder_already_exists_then_the_existing_id_is_looked_up_and_stored()
    {
        graphService.GetFolderIdByPathAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Drive, "/Drafts", Arg.Any<CancellationToken>())
            .Returns(new Ok<Option<string>, string>(Option.None<string>()), new Ok<Option<string>, string>(Option.Some("existing-id")));

        await Run();

        await syncRuleRepository.Received(1).UpsertAsync(new AccountId("user-1"), "/Drafts", RuleType.Include, "existing-id", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_create_fails_and_the_folder_still_cannot_be_found_then_the_rule_is_not_updated()
    {
        graphService.GetFolderIdByPathAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Drive, "/Drafts", Arg.Any<CancellationToken>())
            .Returns(new Ok<Option<string>, string>(Option.None<string>()));

        await Run();

        await syncRuleRepository.DidNotReceive().UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
