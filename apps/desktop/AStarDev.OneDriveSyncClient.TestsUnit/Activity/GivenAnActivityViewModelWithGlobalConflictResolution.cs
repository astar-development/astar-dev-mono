using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Conflicts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Pipeline;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Activity;

public sealed class GivenAnActivityViewModelWithGlobalConflictResolution
{
    private readonly ISyncService _syncService = Substitute.For<ISyncService>();
    private readonly ILocalizationService _loc = Substitute.For<ILocalizationService>();

    public GivenAnActivityViewModelWithGlobalConflictResolution() => _loc.GetLocal(Arg.Any<string>()).Returns(call => call.ArgAt<string>(0));

    private static SyncConflict BuildConflict(string fileName) => new()
    {
        Id = Guid.NewGuid(),
        Remote = RemoteItemRefFactory.Create(new AccountId("acc-1"), new OneDriveFolderId("folder-1"), new OneDriveItemId(fileName)),
        Target = SyncFileTargetFactory.Create($"/home/user/{fileName}", fileName),
        Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow.AddHours(-1), 1024L, DateTimeOffset.UtcNow, 2048L),
    };

    private ActivityViewModel CreateSut(params string[] conflictFileNames)
    {
        var syncRepository = Substitute.For<ISyncRepository>();
        syncRepository.GetPendingConflictsAsync(Arg.Any<AccountId>()).Returns([]);
        var sut = new ActivityViewModel(syncRepository, Substitute.For<ISyncEventAggregator>(), new ConflictItemViewModelFactory(_syncService, _loc), new ActivityItemViewModelFactory(_loc), new InlineUiDispatcher(), _loc);

        foreach (string fileName in conflictFileNames)
            sut.AddConflictItem(BuildConflict(fileName));

        return sut;
    }

    [Fact]
    public void when_created_then_ignore_is_the_only_selected_global_policy()
    {
        var sut = CreateSut();

        sut.GlobalPolicyOptions.Where(option => option.IsSelected).Select(option => option.Policy).ShouldBe([ConflictPolicy.Ignore]);
    }

    [Fact]
    public void when_global_policy_selected_then_only_that_option_is_selected()
    {
        var sut = CreateSut();

        sut.SelectGlobalPolicyCommand.Execute(ConflictPolicy.RemoteWins);

        sut.GlobalPolicyOptions.Where(option => option.IsSelected).Select(option => option.Policy).ShouldBe([ConflictPolicy.RemoteWins]);
    }

    [Fact]
    public void when_global_policy_selected_then_selected_global_policy_matches()
    {
        var sut = CreateSut();

        sut.SelectGlobalPolicyCommand.Execute(ConflictPolicy.LocalWins);

        sut.SelectedGlobalPolicy.ShouldBe(ConflictPolicy.LocalWins);
    }

    [Fact]
    public async Task when_resolve_all_is_executed_then_every_conflict_is_resolved_with_the_global_policy()
    {
        var sut = CreateSut("a.txt", "b.txt", "c.txt");
        sut.SelectGlobalPolicyCommand.Execute(ConflictPolicy.RemoteWins);

        await sut.ResolveAllConflictsCommand.ExecuteAsync(null);

        await _syncService.Received(3).ResolveConflictAsync(Arg.Any<SyncConflict>(), ConflictPolicy.RemoteWins, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_resolve_all_is_executed_then_no_conflicts_remain()
    {
        var sut = CreateSut("a.txt", "b.txt");

        await sut.ResolveAllConflictsCommand.ExecuteAsync(null);

        sut.Conflicts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_resolve_all_is_executed_then_conflict_count_is_zero()
    {
        var sut = CreateSut("a.txt", "b.txt");

        await sut.ResolveAllConflictsCommand.ExecuteAsync(null);

        sut.ConflictCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_resolve_all_is_executed_with_no_conflicts_then_nothing_is_resolved()
    {
        var sut = CreateSut();

        await sut.ResolveAllConflictsCommand.ExecuteAsync(null);

        await _syncService.DidNotReceive().ResolveConflictAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictPolicy>(), Arg.Any<CancellationToken>());
    }
}
