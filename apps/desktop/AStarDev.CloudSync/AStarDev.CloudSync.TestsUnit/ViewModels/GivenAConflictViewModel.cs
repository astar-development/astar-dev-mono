using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.ViewModels;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenAConflictViewModel
{
    [Fact]
    public void when_created_then_the_file_name_detail_and_actions_are_localised()
    {
        var conflict = SyncConflictFactory.Create(SyncPairIdFactory.Create(), "holiday-042.jpg", TestData.Now);

        var viewModel = new ConflictViewModel(conflict, "Holiday photos", TestData.CreateLocalization());

        viewModel.FileName.ShouldBe("holiday-042.jpg");
        viewModel.Detail.ShouldBe("Holiday photos · changed on both sides");
        viewModel.KeepBothText.ShouldBe("Keep both");
        viewModel.KeepLocalText.ShouldBe("Keep local");
        viewModel.KeepCloudText.ShouldBe("Keep cloud");
    }
}
