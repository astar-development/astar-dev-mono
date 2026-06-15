using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Detection;

public sealed class GivenARemoteFolderEnumerator
{
    private readonly IGraphService         _graphService         = Substitute.For<IGraphService>();
    private readonly ISyncRuleRepository   _syncRuleRepository   = Substitute.For<ISyncRuleRepository>();
    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();

    public GivenARemoteFolderEnumerator()
    {
        _syncedItemRepository.GetAllByAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
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

    private static async IAsyncEnumerable<DeltaItem> EmptyStream()
    {
        await Task.CompletedTask;
        yield break;
    }

    [Fact]
    public async Task when_no_rules_configured_then_context_had_no_rules_flag_is_set()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var context = new RemoteEnumerationContext();

        await foreach (var _ in CreateSut().StreamAsync(CreateAccount(), tokenFactory, context, ct: TestContext.Current.CancellationToken)) { }

        context.HadNoRules.ShouldBeTrue();
    }

    [Fact]
    public async Task when_no_rules_configured_then_graph_drive_id_is_not_requested()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var context = new RemoteEnumerationContext();

        await foreach (var _ in CreateSut().StreamAsync(CreateAccount(), tokenFactory, context, ct: TestContext.Current.CancellationToken)) { }

        await _graphService.DidNotReceive().GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_no_rules_configured_then_streamed_items_is_empty()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var context = new RemoteEnumerationContext();
        var items = new List<DeltaItem>();

        await foreach (var item in CreateSut().StreamAsync(CreateAccount(), tokenFactory, context, ct: TestContext.Current.CancellationToken))
            items.Add(item);

        items.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_folder_id_cannot_be_resolved_then_enumerate_folder_is_not_called()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents")]);
        _graphService.GetFolderIdByPathAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var context = new RemoteEnumerationContext();

        await foreach (var _ in CreateSut().StreamAsync(CreateAccount(), tokenFactory, context, ct: TestContext.Current.CancellationToken)) { }

        await _graphService.DidNotReceive().EnumerateFolderAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_folder_id_resolved_from_graph_then_sync_rule_repository_upsert_is_called_to_back_fill()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: null)]);
        _graphService.GetFolderIdByPathAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("folder-resolved");
        _graphService.EnumerateFolderAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DeltaItem>, string>.Ok([]));
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var context = new RemoteEnumerationContext();

        await foreach (var _ in CreateSut().StreamAsync(CreateAccount(), tokenFactory, context, ct: TestContext.Current.CancellationToken)) { }

        await _syncRuleRepository.Received(1).UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Is(RuleType.Include), Arg.Is("folder-resolved"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_rule_already_has_matching_remote_item_id_then_sync_rule_upsert_is_not_called()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DeltaItem>, string>.Ok([]));
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var context = new RemoteEnumerationContext();

        await foreach (var _ in CreateSut().StreamAsync(CreateAccount(), tokenFactory, context, ct: TestContext.Current.CancellationToken)) { }

        await _syncRuleRepository.DidNotReceive().UpsertAsync(Arg.Any<AccountId>(), Arg.Any<string>(), Arg.Any<RuleType>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_enumeration_returns_items_then_context_seen_remote_ids_contains_all_item_ids()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DeltaItem>, string>.Ok([FileItem("item-a", "a.txt", "/Documents/a.txt"), FileItem("item-b", "b.txt", "/Documents/b.txt")]));
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var context = new RemoteEnumerationContext();

        await foreach (var _ in CreateSut().StreamAsync(CreateAccount(), tokenFactory, context, ct: TestContext.Current.CancellationToken)) { }

        context.SeenRemoteIds.ShouldContain("item-a");
        context.SeenRemoteIds.ShouldContain("item-b");
    }

    [Fact]
    public async Task when_enumeration_returns_items_then_streamed_items_contains_all_returned_items()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DeltaItem>, string>.Ok([FileItem("item-a", "a.txt", "/Documents/a.txt"), FileItem("item-b", "b.txt", "/Documents/b.txt")]));
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var context = new RemoteEnumerationContext();
        var items = new List<DeltaItem>();

        await foreach (var item in CreateSut().StreamAsync(CreateAccount(), tokenFactory, context, ct: TestContext.Current.CancellationToken))
            items.Add(item);

        items.Count.ShouldBe(2);
        items.ShouldContain(i => i.Id.Id == "item-a");
        items.ShouldContain(i => i.Id.Id == "item-b");
    }

    [Fact]
    public async Task when_stream_completes_then_context_rules_contains_all_configured_rules()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.EnumerateFolderAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>())
            .Returns(new Result<List<DeltaItem>, string>.Ok([]));
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var context = new RemoteEnumerationContext();

        await foreach (var _ in CreateSut().StreamAsync(CreateAccount(), tokenFactory, context, ct: TestContext.Current.CancellationToken)) { }

        context.Rules.ShouldHaveSingleItem();
        context.Rules[0].RemotePath.ShouldBe("/Documents");
    }

    [Fact]
    public async Task when_cancellation_requested_during_rule_loop_then_partial_items_are_yielded()
    {
        using var cts = new CancellationTokenSource();
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1"), IncludeRule("/Pictures", remoteItemId: "folder-2")]);
        _graphService.EnumerateFolderAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Is("folder-1"), Arg.Any<string>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                cts.Cancel();
                return new Result<List<DeltaItem>, string>.Ok([FileItem("item-a", "a.txt", "/Documents/a.txt")]);
            });
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var context = new RemoteEnumerationContext();
        var items = new List<DeltaItem>();

        await foreach (var item in CreateSut().StreamAsync(CreateAccount(), tokenFactory, context, ct: cts.Token))
            items.Add(item);

        items.ShouldHaveSingleItem();
        items[0].Id.Id.ShouldBe("item-a");
    }

    [Fact]
    public async Task when_drive_id_resolution_fails_then_context_had_no_rules_is_false()
    {
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns([IncludeRule("/Documents", remoteItemId: "folder-1")]);
        _graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DriveId, string>.Error("drive-id-resolution-failed"));
        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult("token");
        var context = new RemoteEnumerationContext();

        await foreach (var _ in CreateSut().StreamAsync(CreateAccount(), tokenFactory, context, ct: TestContext.Current.CancellationToken)) { }

        context.HadNoRules.ShouldBeFalse();
    }
}
