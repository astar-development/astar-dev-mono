using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAConflictResolutionPlanner
{
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly DateTimeOffset Moment = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

    private readonly ConflictResolutionPlanner planner = new();

    public static TheoryData<string, string, string> Cells => new()
    {
        { "BothModified", "LocalWins", "UploadReplacingRemote" },
        { "BothModified", "RemoteWins", "DownloadOverLocal" },
        { "BothModified", "KeepBoth", "KeepBothUploadAndDownload" },
        { "BothModified", "Skip", "LeaveOpen" },
        { "BothAdded", "LocalWins", "UploadReplacingRemote" },
        { "BothAdded", "RemoteWins", "DownloadOverLocal" },
        { "BothAdded", "KeepBoth", "KeepBothUploadAndDownload" },
        { "BothAdded", "Skip", "LeaveOpen" },
        { "RemoteChangedDuringTransfer", "LocalWins", "UploadReplacingRemote" },
        { "RemoteChangedDuringTransfer", "RemoteWins", "DownloadOverLocal" },
        { "RemoteChangedDuringTransfer", "KeepBoth", "KeepBothUploadAndDownload" },
        { "RemoteChangedDuringTransfer", "Skip", "LeaveOpen" },
        { "LocalModifiedRemoteDeleted", "LocalWins", "UploadAsNewFile" },
        { "LocalModifiedRemoteDeleted", "RemoteWins", "TrashLocal" },
        { "LocalModifiedRemoteDeleted", "KeepBoth", "UploadAsNewFile" },
        { "LocalModifiedRemoteDeleted", "Skip", "LeaveOpen" },
        { "LocalDeletedRemoteModified", "LocalWins", "DeleteRemote" },
        { "LocalDeletedRemoteModified", "RemoteWins", "DownloadOverLocal" },
        { "LocalDeletedRemoteModified", "KeepBoth", "DownloadOverLocal" },
        { "LocalDeletedRemoteModified", "Skip", "LeaveOpen" },
        { "MoveTargetTaken", "LocalWins", "KeepLocalMoveRemoteAside" },
        { "MoveTargetTaken", "RemoteWins", "ApplyRemoteMoveRenameLocal" },
        { "MoveTargetTaken", "KeepBoth", "KeepBothAtMoveTarget" },
        { "MoveTargetTaken", "Skip", "LeaveOpen" },
    };

    [Theory]
    [MemberData(nameof(Cells))]
    public void when_a_strategy_is_planned_for_a_kind_then_the_plan_is_the_cell_of_the_strategy_table(string kind, string strategy, string expected)
    {
        var item = Conflict(KindOf(kind), Present(), Present());

        var plan = planner.Plan(item, StrategyOf(strategy));

        plan.Match(found => found.GetType().Name, reason => reason.GetType().Name).ShouldBe(expected);
    }

    [Fact]
    public void when_the_item_is_a_held_deletion_set_then_every_strategy_is_not_allowed_as_not_a_conflict()
    {
        var held = SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(Guid.NewGuid()), Account, SelectionTestRules.PairAt("Docs"), DeletionDirectionFactory.CreateDeleteLocal(), [SyncPaths.Of("Docs/a.txt")], Moment);

        var reasons = Strategies().Select(strategy => planner.Plan(held, strategy).Match(_ => "allowed", reason => reason.GetType().Name)).Distinct().ToList();

        reasons.ShouldBe(["NotAConflict"]);
    }

    [Fact]
    public void when_local_wins_needs_a_local_file_and_none_was_recorded_then_it_is_not_allowed_with_local_side_absent()
    {
        var item = Conflict(ConflictKindFactory.CreateBothModified(), SideSnapshotFactory.CreateAbsent(), Present());

        var plan = planner.Plan(item, ConflictStrategyFactory.CreateLocalWins());

        plan.Match(_ => "allowed", reason => reason.GetType().Name).ShouldBe("LocalSideAbsent");
    }

    [Fact]
    public void when_remote_wins_needs_a_cloud_file_and_none_was_recorded_then_it_is_not_allowed_with_remote_side_absent()
    {
        var item = Conflict(ConflictKindFactory.CreateBothModified(), Present(), SideSnapshotFactory.CreateAbsent());

        var plan = planner.Plan(item, ConflictStrategyFactory.CreateRemoteWins());

        plan.Match(_ => "allowed", reason => reason.GetType().Name).ShouldBe("RemoteSideAbsent");
    }

    [Fact]
    public void when_the_cloud_side_is_gone_and_the_strategy_keeps_the_local_file_then_it_is_allowed()
    {
        var item = Conflict(ConflictKindFactory.CreateLocalModifiedRemoteDeleted(), Present(), SideSnapshotFactory.CreateAbsent());

        var plans = new[] { ConflictStrategyFactory.CreateLocalWins(), ConflictStrategyFactory.CreateRemoteWins(), ConflictStrategyFactory.CreateKeepBoth() }.Select(strategy => planner.Plan(item, strategy).Match(found => found.GetType().Name, reason => reason.GetType().Name)).ToList();

        plans.ShouldBe(["UploadAsNewFile", "TrashLocal", "UploadAsNewFile"]);
    }

    [Fact]
    public void when_the_local_file_is_gone_and_the_strategy_downloads_the_cloud_file_then_it_is_allowed()
    {
        var item = Conflict(ConflictKindFactory.CreateLocalDeletedRemoteModified(), SideSnapshotFactory.CreateAbsent(), Present());

        var plans = new[] { ConflictStrategyFactory.CreateLocalWins(), ConflictStrategyFactory.CreateRemoteWins(), ConflictStrategyFactory.CreateKeepBoth() }.Select(strategy => planner.Plan(item, strategy).Match(found => found.GetType().Name, reason => reason.GetType().Name)).ToList();

        plans.ShouldBe(["DeleteRemote", "DownloadOverLocal", "DownloadOverLocal"]);
    }

    [Fact]
    public void when_skip_is_planned_for_a_conflict_with_an_absent_side_then_it_is_still_allowed()
    {
        var item = Conflict(ConflictKindFactory.CreateBothModified(), SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent());

        var plan = planner.Plan(item, ConflictStrategyFactory.CreateSkip());

        plan.Match(found => found.GetType().Name, reason => reason.GetType().Name).ShouldBe("LeaveOpen");
    }

    private static IEnumerable<ConflictStrategy> Strategies() =>
        [ConflictStrategyFactory.CreateLocalWins(), ConflictStrategyFactory.CreateRemoteWins(), ConflictStrategyFactory.CreateKeepBoth(), ConflictStrategyFactory.CreateSkip()];

    private static ConflictKind KindOf(string name) => name switch
    {
        "BothModified" => ConflictKindFactory.CreateBothModified(),
        "BothAdded" => ConflictKindFactory.CreateBothAdded(),
        "RemoteChangedDuringTransfer" => ConflictKindFactory.CreateRemoteChangedDuringTransfer(),
        "LocalModifiedRemoteDeleted" => ConflictKindFactory.CreateLocalModifiedRemoteDeleted(),
        "LocalDeletedRemoteModified" => ConflictKindFactory.CreateLocalDeletedRemoteModified(),
        _ => ConflictKindFactory.CreateMoveTargetTaken(),
    };

    private static ConflictStrategy StrategyOf(string name) => name switch
    {
        "LocalWins" => ConflictStrategyFactory.CreateLocalWins(),
        "RemoteWins" => ConflictStrategyFactory.CreateRemoteWins(),
        "KeepBoth" => ConflictStrategyFactory.CreateKeepBoth(),
        _ => ConflictStrategyFactory.CreateSkip(),
    };

    private static SideSnapshot Present() => SideSnapshotFactory.CreatePresent(10, Moment, Option.None<ContentHash>(), Option.None<ContentTag>());

    private static SyncAttentionItem Conflict(ConflictKind kind, SideSnapshot local, SideSnapshot remote) =>
        SyncAttentionItemFactory.CreateConflict(new ConflictId(Guid.NewGuid()), Account, SyncPaths.Of("Docs/a.txt"), kind, local, remote, Moment);
}
