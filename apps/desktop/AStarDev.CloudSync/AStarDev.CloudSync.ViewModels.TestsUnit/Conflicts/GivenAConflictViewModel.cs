using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.ViewModels.Conflicts;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Conflicts;

public class GivenAConflictViewModel
{
    [Fact]
    public void when_created_then_the_file_name_detail_and_actions_are_localised()
    {
        var conflict = SyncConflictFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), "holiday-042.jpg", TestData.Now);

        var viewModel = new ConflictViewModel(conflict, "Holiday photos", new RecordingSyncActions(), TestData.CreateLocalization());

        viewModel.FileName.ShouldBe("holiday-042.jpg");
        viewModel.Detail.ShouldBe("Holiday photos · changed on both sides");
        viewModel.KeepBothText.ShouldBe("Keep both");
        viewModel.KeepLocalText.ShouldBe("Keep local");
        viewModel.KeepCloudText.ShouldBe("Keep cloud");
    }

    [Fact]
    public async Task when_each_resolution_command_runs_then_the_conflict_is_resolved_that_way()
    {
        var syncActions = new RecordingSyncActions();
        var conflict = SyncConflictFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), "holiday-042.jpg", TestData.Now);
        var viewModel = new ConflictViewModel(conflict, "Holiday photos", syncActions, TestData.CreateLocalization());

        await viewModel.KeepBothCommand.ExecuteAsync(null);
        await viewModel.KeepLocalCommand.ExecuteAsync(null);
        await viewModel.KeepCloudCommand.ExecuteAsync(null);

        syncActions.Calls.ShouldBe([$"KeepBothAsync:{conflict.SyncPairId}:holiday-042.jpg", $"KeepLocalAsync:{conflict.SyncPairId}:holiday-042.jpg", $"KeepCloudAsync:{conflict.SyncPairId}:holiday-042.jpg"]);
    }
}
