using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Settings;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Settings;

public sealed class GivenALanguageOptionFactory
{
    private static ILocalizationService BuildLocWithCultures(params CultureInfo[] cultures)
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.AvailableCultures.Returns(cultures.ToList());

        return loc;
    }

    [Fact]
    public void when_two_cultures_available_then_two_options_are_created()
    {
        var loc = BuildLocWithCultures(CultureInfo.GetCultureInfo("en-GB"), CultureInfo.GetCultureInfo("en-US"));

        var options = LanguageOptionFactory.Create(loc);

        options.Count.ShouldBe(2);
    }

    [Fact]
    public void when_cultures_available_then_each_option_culture_matches_source()
    {
        var enGb = CultureInfo.GetCultureInfo("en-GB");
        var enUs = CultureInfo.GetCultureInfo("en-US");
        var loc = BuildLocWithCultures(enGb, enUs);

        var options = LanguageOptionFactory.Create(loc);

        options.Select(option => option.Culture.Name).ShouldBe(["en-GB", "en-US"]);
    }

    [Fact]
    public void when_cultures_available_then_label_is_native_name_of_culture()
    {
        var enGb = CultureInfo.GetCultureInfo("en-GB");
        var loc = BuildLocWithCultures(enGb);

        var options = LanguageOptionFactory.Create(loc);

        options[0].Label.ShouldBe(enGb.NativeName);
    }

    [Fact]
    public void when_no_cultures_available_then_empty_list_is_returned()
    {
        var loc = BuildLocWithCultures();

        var options = LanguageOptionFactory.Create(loc);

        options.ShouldBeEmpty();
    }
}
