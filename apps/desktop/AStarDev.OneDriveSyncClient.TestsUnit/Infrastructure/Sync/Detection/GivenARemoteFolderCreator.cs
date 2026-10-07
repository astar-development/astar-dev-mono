using System.Collections.Concurrent;
using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Detection;

public sealed class GivenARemoteFolderCreator
{
    private const string SyncRoot = "/sync-root";
    private static readonly DriveId Drive = new("drive-1");

    private readonly IGraphService graphService = Substitute.For<IGraphService>();
    private readonly ISyncRuleRepository syncRuleRepository = Substitute.For<ISyncRuleRepository>();
    private readonly MockFileSystem fileSystem = new();

    public GivenARemoteFolderCreator()
    {
        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Ok<DriveId, string>(Drive));
        graphService.GetFolderIdByPathAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Drive, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        graphService.CreateFolderAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new Ok<DriveFolder, string>(new DriveFolder($"new-{callInfo.ArgAt<string>(3)}", callInfo.ArgAt<string>(3), callInfo.ArgAt<string>(2))));
    }

    private RemoteFolderCreator CreateSut() => new(graphService, syncRuleRepository, fileSystem, Substitute.For<ILogger<RemoteFolderCreator>>());

    private static Func<CancellationToken, Task<string>> TokenFactory => _ => Task.FromResult("token");

    private static OneDriveAccount CreateAccount() => new()
    {
        Id = new AccountId("user-1"),
        Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore(SyncRoot)),
        SelectedFolderIds = []
    };

    private static AccountSyncConfig CreateSyncConfig() => AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore(SyncRoot));

    private static SyncRuleEntity Rule(string remotePath, RuleType ruleType = RuleType.Include, string? remoteItemId = null)
        => new() { RemotePath = remotePath, RuleType = ruleType, RemoteItemId = remoteItemId is null ? Option.None<string>() : Option.Some(remoteItemId) };

    private static RemoteEnumerationContext ContextWith(params SyncRuleEntity[] rules) => new() { Rules = rules };

    private Task Run(RemoteEnumerationContext context) => CreateSut().CreateMissingFoldersAsync(CreateAccount(), CreateSyncConfig(), TokenFactory, context, TestContext.Current.CancellationToken);

    [Fact]
    public async Task when_a_local_only_include_rule_has_no_remote_folder_then_the_folder_is_created_under_the_drive_root()
    {
        fileSystem.Directory.CreateDirectory("/sync-root/Drafts");

        await Run(ContextWith(Rule("/Drafts")));

        await graphService.Received(1).CreateFolderAsync("user-1", Arg.Any<Func<CancellationToken, Task<string>>>(), "root", "Drafts", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_remote_folder_is_created_then_the_new_id_is_stored_on_the_rule()
    {
        fileSystem.Directory.CreateDirectory("/sync-root/Drafts");

        await Run(ContextWith(Rule("/Drafts")));

        await syncRuleRepository.Received(1).UpsertAsync(new AccountId("user-1"), "/Drafts", RuleType.Include, "new-Drafts", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_parent_exists_remotely_then_the_folder_is_created_under_the_parent()
    {
        fileSystem.Directory.CreateDirectory("/sync-root/Docs/Notes");
        graphService.GetFolderIdByPathAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Drive, "/Docs", Arg.Any<CancellationToken>()).Returns("docs-id");

        await Run(ContextWith(Rule("/Docs/Notes")));

        await graphService.Received(1).CreateFolderAsync("user-1", Arg.Any<Func<CancellationToken, Task<string>>>(), "docs-id", "Notes", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_parent_is_also_missing_remotely_then_the_parent_is_created_first()
    {
        fileSystem.Directory.CreateDirectory("/sync-root/Docs/Notes");

        await Run(ContextWith(Rule("/Docs/Notes")));

        Received.InOrder(() =>
        {
            graphService.CreateFolderAsync("user-1", Arg.Any<Func<CancellationToken, Task<string>>>(), "root", "Docs", Arg.Any<CancellationToken>());
            graphService.CreateFolderAsync("user-1", Arg.Any<Func<CancellationToken, Task<string>>>(), "new-Docs", "Notes", Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task when_the_folder_already_exists_remotely_then_it_is_not_created_and_its_id_is_stored()
    {
        fileSystem.Directory.CreateDirectory("/sync-root/Drafts");
        graphService.GetFolderIdByPathAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Drive, "/Drafts", Arg.Any<CancellationToken>()).Returns("existing-id");

        await Run(ContextWith(Rule("/Drafts")));

        await graphService.DidNotReceive().CreateFolderAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await syncRuleRepository.Received(1).UpsertAsync(new AccountId("user-1"), "/Drafts", RuleType.Include, "existing-id", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_rule_already_has_a_remote_id_then_the_graph_is_not_called()
    {
        fileSystem.Directory.CreateDirectory("/sync-root/Photos");

        await Run(ContextWith(Rule("/Photos", remoteItemId: "photos-id")));

        await graphService.DidNotReceive().GetFolderIdByPathAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await graphService.DidNotReceive().CreateFolderAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_rule_is_an_exclude_rule_then_nothing_is_created()
    {
        fileSystem.Directory.CreateDirectory("/sync-root/Drafts");

        await Run(ContextWith(Rule("/Drafts", RuleType.Exclude)));

        await graphService.DidNotReceive().CreateFolderAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_local_folder_does_not_exist_then_nothing_is_created()
    {
        await Run(ContextWith(Rule("/Missing")));

        await graphService.DidNotReceive().CreateFolderAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_folder_is_already_tracked_as_synced_then_the_graph_is_not_called()
    {
        fileSystem.Directory.CreateDirectory("/sync-root/Drafts");
        var context = ContextWith(Rule("/Drafts"));
        context.SyncedItems = new ConcurrentDictionary<string, SyncedItemEntity>(new Dictionary<string, SyncedItemEntity> { ["f1"] = new() { IsFolder = true, RemotePath = "/Drafts", RemoteItemId = new OneDriveItemId("f1") } });

        await Run(context);

        await graphService.DidNotReceive().GetFolderIdByPathAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_creating_a_folder_fails_then_no_id_is_stored_and_later_rules_are_still_processed()
    {
        fileSystem.Directory.CreateDirectory("/sync-root/Bad");
        fileSystem.Directory.CreateDirectory("/sync-root/Good");
        graphService.CreateFolderAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), "Bad", Arg.Any<CancellationToken>())
            .Returns(new Fail<DriveFolder, string>("nameAlreadyExists"));

        await Run(ContextWith(Rule("/Bad"), Rule("/Good")));

        await syncRuleRepository.DidNotReceive().UpsertAsync(Arg.Any<AccountId>(), "/Bad", Arg.Any<RuleType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await syncRuleRepository.Received(1).UpsertAsync(Arg.Any<AccountId>(), "/Good", RuleType.Include, "new-Good", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_drive_id_cannot_be_resolved_then_nothing_is_created()
    {
        fileSystem.Directory.CreateDirectory("/sync-root/Drafts");
        graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Fail<DriveId, string>("no drive"));

        await Run(ContextWith(Rule("/Drafts")));

        await graphService.DidNotReceive().CreateFolderAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_there_are_no_rules_to_create_then_the_drive_id_is_not_requested()
    {
        await Run(ContextWith());

        await graphService.DidNotReceive().GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>());
    }
}
