using AStarDev.CloudSync.TestHelpers.Ui;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Classification;

public class GivenTheClassificationPageViewModel
{
    [Fact]
    public void when_created_then_the_title_is_localised_and_the_load_command_is_the_tabs_load_command()
    {
        var page = TestData.CreateClassificationPage();

        (page.TitleText, page.LoadCommand).ShouldBe(("Classification", page.Tabs.LoadCommand));
    }
}
