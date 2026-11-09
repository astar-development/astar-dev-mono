using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Layouts;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Layouts;

public class GivenAProviderFilterViewModel
{
    [Fact]
    public void when_created_then_the_label_names_the_provider_and_its_pair_count()
    {
        var localization = TestData.CreateLocalization();

        var filter = new ProviderFilterViewModel(new ProviderViewModel(CloudProviderFactory.CreateOneDrive(), localization), 1_234, _ => { }, localization);

        filter.Label.ShouldBe("OneDrive · 1,234");
    }

    [Fact]
    public void when_created_then_the_provider_is_exposed_for_the_chip_colour()
    {
        var localization = TestData.CreateLocalization();

        var filter = new ProviderFilterViewModel(new ProviderViewModel(CloudProviderFactory.CreateOneDrive(), localization), 7, _ => { }, localization);

        filter.Provider.IsOneDrive.ShouldBeTrue();
    }

    [Fact]
    public void when_created_then_it_is_not_selected()
    {
        var localization = TestData.CreateLocalization();

        var filter = new ProviderFilterViewModel(new ProviderViewModel(CloudProviderFactory.CreateOneDrive(), localization), 7, _ => { }, localization);

        filter.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void when_the_select_command_runs_then_the_filter_is_passed_to_the_select_callback()
    {
        var localization = TestData.CreateLocalization();
        var selected = new List<ProviderFilterViewModel>();
        var filter = new ProviderFilterViewModel(new ProviderViewModel(CloudProviderFactory.CreateOneDrive(), localization), 7, selected.Add, localization);

        filter.SelectCommand.Execute(null);

        selected.ShouldHaveSingleItem().ShouldBeSameAs(filter);
    }

    [Fact]
    public async Task when_a_pair_uses_the_filters_provider_then_it_matches()
    {
        var localization = TestData.CreateLocalization();
        var overview = await TestData.CreateLoadedOverviewViewModelAsync();
        var filter = new ProviderFilterViewModel(new ProviderViewModel(CloudProviderFactory.CreateOneDrive(), localization), 7, _ => { }, localization);

        var matches = filter.Matches(overview.SyncPairs[0]);

        matches.ShouldBeTrue();
    }
}
