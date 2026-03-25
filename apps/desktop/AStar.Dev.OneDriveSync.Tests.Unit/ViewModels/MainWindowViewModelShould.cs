using AStar.Dev.OneDriveSync.Theming;
using AStar.Dev.OneDriveSync.ViewModels;

namespace AStar.Dev.OneDriveSync.Tests.Unit.ViewModels;

[TestSubject(typeof(MainWindowViewModel))]
public class MainWindowViewModelShould
{
    private readonly IThemeService _themeService = Substitute.For<IThemeService>();
    private readonly MainWindowViewModel _sut;

    public MainWindowViewModelShould()
    {
        _themeService.CurrentMode.Returns(ThemeMode.Auto);
        _sut = new MainWindowViewModel(_themeService);
    }

    [Fact]
    public void InitialiseThreeOptions_OnConstruction()
        => _sut.ThemeOptions.Count.ShouldBe(3);

    [Fact]
    public void ExposeOptionsWithCorrectDisplayNames()
    {
        _sut.ThemeOptions.Select(o => o.DisplayName)
            .ShouldBe(["Light", "Dark", "System"], ignoreOrder: false);
    }

    [Fact]
    public void SelectOptionMatchingCurrentMode_OnConstruction()
        => _sut.SelectedThemeOption.Mode.ShouldBe(ThemeMode.Auto);

    [Fact]
    public void ApplyNewTheme_WhenOptionChanged()
    {
        var darkOption = _sut.ThemeOptions.First(o => o.Mode == ThemeMode.Dark);

        _sut.SelectedThemeOption = darkOption;

        _themeService.Received(1).Apply(ThemeMode.Dark);
    }
}
