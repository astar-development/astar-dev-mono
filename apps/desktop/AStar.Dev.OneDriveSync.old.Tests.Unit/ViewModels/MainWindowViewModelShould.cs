using AStar.Dev.OneDriveSync.old.Services;
using AStar.Dev.OneDriveSync.old.Theming;
using AStar.Dev.OneDriveSync.old.Logging;
using AStar.Dev.OneDriveSync.old.ViewModels;

namespace AStar.Dev.OneDriveSync.old.Tests.Unit.ViewModels;

[TestSubject(typeof(MainWindowViewModel))]
public class MainWindowViewModelShould
{
    private readonly IThemeService _themeService = Substitute.For<IThemeService>();
    private readonly MainWindowViewModel _sut;

    public MainWindowViewModelShould()
    {
        _themeService.CurrentMode.Returns(ThemeMode.Auto);

        var loggingService = new LoggingService();
        var accountStore = Substitute.For<IAccountStore>();
        accountStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<Models.AccountRecord>>([]));
        var authService = Substitute.For<IMsalAuthService>();
        var folderService = Substitute.For<IOneDriveFolderService>();

        _sut = new MainWindowViewModel(_themeService, loggingService, accountStore, authService, folderService);
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
