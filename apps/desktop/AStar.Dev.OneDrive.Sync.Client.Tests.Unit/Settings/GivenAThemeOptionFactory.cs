using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Settings;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Settings;

public sealed class GivenAThemeOptionFactory
{
    private static ILocalizationService BuildLocalizationService()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));

        return loc;
    }

    [Fact]
    public void when_create_called_then_returns_four_options()
    {
        var loc = BuildLocalizationService();

        var options = ThemeOptionFactory.Create(loc);

        options.Count.ShouldBe(4);
    }

    [Fact]
    public void when_create_called_then_all_theme_enum_values_are_covered()
    {
        var loc = BuildLocalizationService();

        var options = ThemeOptionFactory.Create(loc);

        var themes = options.Select(option => option.Theme).ToList();
        themes.ShouldContain(AppTheme.Light);
        themes.ShouldContain(AppTheme.Dark);
        themes.ShouldContain(AppTheme.System);
        themes.ShouldContain(AppTheme.Hacker);
    }

    [Fact]
    public void when_create_called_then_hacker_uses_correct_localisation_key()
    {
        var loc = BuildLocalizationService();

        _ = ThemeOptionFactory.Create(loc);

        loc.Received(1).GetLocal("Settings.Theme.Hacker");
    }
}
