using Avalonia.Styling;
using AStar.Dev.OneDriveSync.Theming;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Theming;

[TestSubject(typeof(ThemeService))]
public class ThemeServiceShould
{
    private readonly IApplicationThemeAdapter _adapter = Substitute.For<IApplicationThemeAdapter>();
    private readonly ThemeService _sut;

    public ThemeServiceShould() => _sut = new ThemeService(_adapter);

    [Fact]
    public void DefaultToAutoMode_OnConstruction()
        => _sut.CurrentMode.ShouldBe(ThemeMode.Auto);

    [Fact]
    public void MapLightMode_ToLightVariant()
        => _sut.ToVariant(ThemeMode.Light).ShouldBe(ThemeVariant.Light);

    [Fact]
    public void MapDarkMode_ToDarkVariant()
        => _sut.ToVariant(ThemeMode.Dark).ShouldBe(ThemeVariant.Dark);

    [Fact]
    public void MapAutoMode_ToDefaultVariant()
        => _sut.ToVariant(ThemeMode.Auto).ShouldBe(ThemeVariant.Default);

    [Fact]
    public void UpdateCurrentMode_WhenApplied()
    {
        _sut.Apply(ThemeMode.Light);

        _sut.CurrentMode.ShouldBe(ThemeMode.Light);
    }

    [Fact]
    public void DelegateVariantToAdapter_WhenApplied()
    {
        _sut.Apply(ThemeMode.Dark);

        _adapter.Received(1).SetThemeVariant(ThemeVariant.Dark);
    }
}
