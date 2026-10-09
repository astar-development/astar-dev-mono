using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Detection;

public sealed class GivenARemoteFolderEnumeratorUsingTheChangeGate
{
    private readonly IGraphService _graphService = Substitute.For<IGraphService>();
    private readonly ISyncRuleRepository _syncRuleRepository = Substitute.For<ISyncRuleRepository>();
    private readonly ISyncedItemRepository _syncedItemRepository = Substitute.For<ISyncedItemRepository>();
    private readonly IRemoteChangeGate _remoteChangeGate = Substitute.For<IRemoteChangeGate>();

    public GivenARemoteFolderEnumeratorUsingTheChangeGate()
    {
        _syncedItemRepository.GetAllByAccountAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns([]);
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns([new SyncRuleEntity { RemotePath = "/Documents", RuleType = RuleType.Include, RemoteItemId = Option.Some("folder-1") }]);
        _graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Ok<DriveId, string>(new DriveId("drive-1")));
        _graphService.EnumerateFolderAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>()).Returns(ItemStream(FileItem("item-a")));
    }

    private RemoteFolderEnumerator CreateSut() => new(_graphService, _syncRuleRepository, _syncedItemRepository, _remoteChangeGate, Substitute.For<ILogger<RemoteFolderEnumerator>>());

    private void GivenDecision(RemoteWalkDecision decision)
        => _remoteChangeGate.DecideAsync(Arg.Any<OneDriveAccount>(), Arg.Any<DriveId>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<IReadOnlyList<SyncRuleEntity>>(), Arg.Any<IReadOnlyDictionary<string, SyncedItemEntity>>(), Arg.Any<CancellationToken>()).Returns(decision);

    private static OneDriveAccount CreateAccount() => new()
    {
        Id = new AccountId("user-1"),
        Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/sync-root")),
        SelectedFolderIds = []
    };

    private static FileDeltaItem FileItem(string id)
        => DeltaItemFactory.CreateFile(new OneDriveItemId(id), new DriveId("drive-1"), Option.None<OneDriveFolderId>(), ItemPathFactory.Create(id, $"/Documents/{id}"), 100L, DateTimeOffset.UtcNow.AddDays(-1), Option.None<string>(), VersionInfoFactory.Create(Option.None<string>(), Option.None<string>()));

    private static async IAsyncEnumerable<DeltaItem> ItemStream(params DeltaItem[] items)
    {
        foreach (var item in items)
            yield return item;

        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<DeltaItem> FailingStream()
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("boom");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    private async Task<(List<DeltaItem> Items, RemoteEnumerationContext Context)> RunAsync()
    {
        var context = new RemoteEnumerationContext();
        List<DeltaItem> items = [];

        await foreach (var item in CreateSut().StreamAsync(CreateAccount(), _ => Task.FromResult("token"), context, cancellationToken: TestContext.Current.CancellationToken))
            items.Add(item);

        return (items, context);
    }

    [Fact]
    public async Task when_the_gate_skips_the_walk_then_no_items_are_yielded_and_nothing_is_enumerated()
    {
        GivenDecision(RemoteWalkDecisionFactory.CreateSkip("next"));

        var (items, _) = await RunAsync();

        items.ShouldBeEmpty();
        _graphService.DidNotReceive().EnumerateFolderAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_gate_skips_the_walk_then_the_decision_is_recorded_on_the_context()
    {
        var decision = RemoteWalkDecisionFactory.CreateSkip("next");
        GivenDecision(decision);

        var (_, context) = await RunAsync();

        context.WalkDecision.ShouldBe(Option.Some(decision));
    }

    [Fact]
    public async Task when_the_gate_requires_a_walk_then_items_are_yielded_and_the_decision_is_recorded()
    {
        var decision = RemoteWalkDecisionFactory.CreateWalk(WalkReasons.NoDeltaLink, Option.Some("latest"));
        GivenDecision(decision);

        var (items, context) = await RunAsync();

        items.Count.ShouldBe(1);
        context.WalkDecision.ShouldBe(Option.Some(decision));
    }

    [Fact]
    public async Task when_a_walk_completes_without_errors_then_no_enumeration_failure_is_recorded()
    {
        GivenDecision(RemoteWalkDecisionFactory.CreateWalk(WalkReasons.NoDeltaLink, Option.None<string>()));

        var (_, context) = await RunAsync();

        context.HadEnumerationFailures.ShouldBeFalse();
    }

    [Fact]
    public async Task when_enumeration_throws_then_an_enumeration_failure_is_recorded()
    {
        GivenDecision(RemoteWalkDecisionFactory.CreateWalk(WalkReasons.NoDeltaLink, Option.None<string>()));
        _graphService.EnumerateFolderAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Action<int>?>(), Arg.Any<CancellationToken>()).Returns(FailingStream());

        var (_, context) = await RunAsync();

        context.HadEnumerationFailures.ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_rule_folder_cannot_be_resolved_then_an_enumeration_failure_is_recorded()
    {
        GivenDecision(RemoteWalkDecisionFactory.CreateWalk(WalkReasons.NoDeltaLink, Option.None<string>()));
        _syncRuleRepository.GetByAccountIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns([new SyncRuleEntity { RemotePath = "/Missing", RuleType = RuleType.Include }]);
        _graphService.GetFolderIdByPathAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        var (_, context) = await RunAsync();

        context.HadEnumerationFailures.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_drive_cannot_be_resolved_then_an_enumeration_failure_is_recorded()
    {
        _graphService.GetDriveIdAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<CancellationToken>()).Returns(new Fail<DriveId, string>("no drive"));

        var (_, context) = await RunAsync();

        context.HadEnumerationFailures.ShouldBeTrue();
    }
}
