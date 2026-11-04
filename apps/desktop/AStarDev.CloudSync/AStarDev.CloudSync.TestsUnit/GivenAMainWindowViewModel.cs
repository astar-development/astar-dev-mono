using AStarDev.CloudSync.ViewModels;
using AStarDev.Localization;

namespace AStarDev.CloudSync.TestsUnit;

public class GivenAMainWindowViewModel
{
    [Fact]
    public void when_created_then_the_title_comes_from_the_app_title_localisation_key()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal("App.Title").Returns("Localised title");

        var viewModel = new MainWindowViewModel(localization);

        viewModel.Title.ShouldBe("Localised title");
    }
}
