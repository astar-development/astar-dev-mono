using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenARemoteFolderEnumerator
{
    private readonly IGraphService         _graphService         = Substitute.For<IGraphService>();
    private readonly ISyncRuleRepository   _syncRuleRepository   = Substitute.For<ISyncRuleRepository>();
    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();

    public GivenARemoteFolderEnumerator()
    {
        _syncedItemRepository.GetAllByAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DriveId, string>.Ok(new DriveId("drive-1")));
    }

    private RemoteFolderEnumerator CreateSut() => new(_graphService, _syncRuleRepository, _syncedItemRepository, Substitute.For<ILogger<RemoteFolderEnumerator>>());

    private static OneDriveAccount CreateAccount() => new()
    {
        Id                = new AccountId("user-1"),
        Profile           = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig        = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/sync-root")),
        SelectedFolderIds = []
    };

    private static SyncRuleEntity IncludeRule(string remotePath, string? remoteItemId = null)
        => new() { RemotePath = remotePath, RuleType = RuleType.Include, RemoteItemId = remoteItemId is null ? Option.None<string>() : Option.Some(remoteItemId) };

    private static FileDeltaItem FileItem(string id, string name, string? relativePath = null)
        => DeltaItemFactory.CreateFile(new OneDriveItemId(id), new DriveId("drive-1"), null, ItemPathFactory.Create(name, relativePath ?? name), 100L, DateTimeOffset.UtcNow.AddDays(-1), null, VersionInfoFactory.Create(null, null));

    [Fact]
    public async Task when_no_rules_configured_then_result_has_no_rules_flag_set()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await CreateSut().EnumerateAsync(CreateAccount(), "token", TestContext.Current.CancellationToken);

        result.HadNoRules.ShouldBeTrue();
    }

    [Fact]
    public async Task when_no_rules_configured_then_graph_drive_id_is_not_requested()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateSut().EnumerateAsync(CreateAccount(), "token", TestContext.Current.CancellationToken);

        await _graphService.DidNotReceive().GetDriveIdAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_no_rules_configured_then_delta_items_is_empty()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await CreateSut().EnumerateAsync(CreateAccount(), "token", TestContext.Current.CancellationToken);

        result.DeltaItems.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_folder_id_cannot_be_resolved_then_enumerate_folder_is_not_called()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents")]);
        _graphService.GetFolderIdByPathAsync(Arg.Any<string>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        await CreateSut().EnumerateAsync(CreateAccount(), "token", TestContext.Current.CancellationToken);

        await _graphService.DidNotReceive().EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_folder_id_resolved_from_graph_then_sync_rule_repository_upsert_is_called_to_back_fill()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: null)]);
        _graphService.GetFolderIdByPathAsync(Arg.Any<string>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("folder-resolved");
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DeltaItem>, string>.Ok([]));

        await CreateSut().EnumerateAsync(CreateAccount(), "token", TestContext.Current.CancellationToken);

        await _syncRuleRepository.Received(1).UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Is(RuleType.Include), Arg.Is("folder-resolved"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_rule_already_has_matching_remote_item_id_then_sync_rule_upsert_is_not_called()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DeltaItem>, string>.Ok([]));

        await CreateSut().EnumerateAsync(CreateAccount(), "token", TestContext.Current.CancellationToken);

        await _syncRuleRepository.DidNotReceive().UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_enumeration_returns_items_then_seen_remote_ids_contains_all_item_ids()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DeltaItem>, string>.Ok([FileItem("item-a", "a.txt", "/Documents/a.txt"), FileItem("item-b", "b.txt", "/Documents/b.txt")]));

        var result = await CreateSut().EnumerateAsync(CreateAccount(), "token", TestContext.Current.CancellationToken);

        result.SeenRemoteIds.ShouldContain("item-a");
        result.SeenRemoteIds.ShouldContain("item-b");
    }

    [Fact]
    public async Task when_enumeration_returns_items_then_delta_items_contains_all_returned_items()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DeltaItem>, string>.Ok([FileItem("item-a", "a.txt", "/Documents/a.txt"), FileItem("item-b", "b.txt", "/Documents/b.txt")]));

        var result = await CreateSut().EnumerateAsync(CreateAccount(), "token", TestContext.Current.CancellationToken);

        result.DeltaItems.Count.ShouldBe(2);
        result.DeltaItems.ShouldContain(i => i.Id.Id == "item-a");
        result.DeltaItems.ShouldContain(i => i.Id.Id == "item-b");
    }

    [Fact]
    public async Task when_result_returned_then_rules_are_included_in_result()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<string>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DeltaItem>, string>.Ok([]));

        var result = await CreateSut().EnumerateAsync(CreateAccount(), "token", TestContext.Current.CancellationToken);

        result.Rules.ShouldHaveSingleItem();
        result.Rules[0].RemotePath.ShouldBe("/Documents");
    }
}
