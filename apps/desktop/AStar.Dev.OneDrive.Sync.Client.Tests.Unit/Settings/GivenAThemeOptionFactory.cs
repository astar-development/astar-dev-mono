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
        var options = ThemeOptionFactory.Create(BuildLocalizationService(), AppTheme.Light);

        options.Count.ShouldBe(4);
    }

    [Fact]
    public void when_create_called_then_all_theme_enum_values_are_covered()
    {
        var options = ThemeOptionFactory.Create(BuildLocalizationService(), AppTheme.Light);

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

        _ = ThemeOptionFactory.Create(loc, AppTheme.Light);

        loc.Received(1).GetLocal("Settings.Theme.Hacker");
    }

    [Fact]
    public void when_selected_theme_is_dark_then_dark_option_is_selected()
    {
        var options = ThemeOptionFactory.Create(BuildLocalizationService(), AppTheme.Dark);

        options.Single(option => option.Theme == AppTheme.Dark).IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void when_selected_theme_is_dark_then_other_options_are_not_selected()
    {
        var options = ThemeOptionFactory.Create(BuildLocalizationService(), AppTheme.Dark);

        options.Where(option => option.Theme != AppTheme.Dark).ShouldAllBe(option => !option.IsSelected);
    }

    [Fact]
    public void when_selected_theme_is_light_then_light_option_is_selected()
    {
        var options = ThemeOptionFactory.Create(BuildLocalizationService(), AppTheme.Light);

        options.Single(option => option.Theme == AppTheme.Light).IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void when_selected_theme_is_hacker_then_hacker_option_is_selected()
    {
        var options = ThemeOptionFactory.Create(BuildLocalizationService(), AppTheme.Hacker);

        options.Single(option => option.Theme == AppTheme.Hacker).IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void when_selected_theme_is_system_then_system_option_is_selected()
    {
        var options = ThemeOptionFactory.Create(BuildLocalizationService(), AppTheme.System);

        options.Single(option => option.Theme == AppTheme.System).IsSelected.ShouldBeTrue();
    }
}
