using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Classification;

public class GivenTheClassificationPageViewModel
{
    [Fact]
    public void when_created_then_the_title_is_localised()
    {
        var page = TestData.CreateClassificationPage();

        page.TitleText.ShouldBe("Classification");
    }

    [Fact]
    public async Task when_loaded_then_the_term_list_loads_and_the_reclassify_freshness_is_checked()
    {
        var freshness = new FakeReclassifyFreshnessCheck { Outcome = Result.Success<ReclassifyFreshness, ClassificationFailure>(ReclassifyFreshnessFactory.CreateStale()) };
        var page = TestData.CreateClassificationPage(new FakeClassificationReclassifier(), freshness);

        await page.LoadCommand.ExecuteAsync(null);

        (page.Tabs.LoadCommand.ExecutionTask is not null, page.Reclassify.IsRerunRecommended).ShouldBe((true, true));
    }
}
