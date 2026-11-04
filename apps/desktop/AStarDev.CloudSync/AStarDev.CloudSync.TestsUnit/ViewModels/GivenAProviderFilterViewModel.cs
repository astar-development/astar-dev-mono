using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.ViewModels;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenAProviderFilterViewModel
{
    [Fact]
    public void when_created_then_the_label_names_the_provider_and_its_pair_count()
    {
        var localization = TestData.CreateLocalization();

        var filter = new ProviderFilterViewModel(new ProviderViewModel(CloudProviderFactory.CreateOneDrive(), localization), 1_234, localization);

        filter.Label.ShouldBe("OneDrive · 1,234");
    }

    [Fact]
    public void when_created_then_the_provider_is_exposed_for_the_chip_colour()
    {
        var localization = TestData.CreateLocalization();

        var filter = new ProviderFilterViewModel(new ProviderViewModel(CloudProviderFactory.CreateOneDrive(), localization), 7, localization);

        filter.Provider.IsOneDrive.ShouldBeTrue();
    }
}
