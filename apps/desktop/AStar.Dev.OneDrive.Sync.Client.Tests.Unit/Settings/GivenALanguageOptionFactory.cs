using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Settings;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Settings;

public sealed class GivenALanguageOptionFactory
{
    private static ILocalizationService BuildLocWithCultures(CultureInfo currentCulture, params CultureInfo[] cultures)
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.AvailableCultures.Returns(cultures.ToList());
        loc.CurrentCulture.Returns(currentCulture);

        return loc;
    }

    [Fact]
    public void when_two_cultures_available_then_two_options_are_created()
    {
        var enGb = CultureInfo.GetCultureInfo("en-GB");
        var loc = BuildLocWithCultures(enGb, enGb, CultureInfo.GetCultureInfo("en-US"));

        var options = LanguageOptionFactory.Create(loc);

        options.Count.ShouldBe(2);
    }

    [Fact]
    public void when_cultures_available_then_each_option_culture_matches_source()
    {
        var enGb = CultureInfo.GetCultureInfo("en-GB");
        var enUs = CultureInfo.GetCultureInfo("en-US");
        var loc = BuildLocWithCultures(enGb, enGb, enUs);

        var options = LanguageOptionFactory.Create(loc);

        options.Select(option => option.Culture.Name).ShouldBe(["en-GB", "en-US"]);
    }

    [Fact]
    public void when_cultures_available_then_label_is_native_name_of_culture()
    {
        var enGb = CultureInfo.GetCultureInfo("en-GB");
        var loc = BuildLocWithCultures(enGb, enGb);

        var options = LanguageOptionFactory.Create(loc);

        options[0].Label.ShouldBe(enGb.NativeName);
    }

    [Fact]
    public void when_no_cultures_available_then_empty_list_is_returned()
    {
        var loc = BuildLocWithCultures(CultureInfo.GetCultureInfo("en-GB"));

        var options = LanguageOptionFactory.Create(loc);

        options.ShouldBeEmpty();
    }

    [Fact]
    public void when_current_culture_is_en_gb_then_en_gb_option_is_selected()
    {
        var enGb = CultureInfo.GetCultureInfo("en-GB");
        var enUs = CultureInfo.GetCultureInfo("en-US");
        var loc = BuildLocWithCultures(enGb, enGb, enUs);

        var options = LanguageOptionFactory.Create(loc);

        options.Single(option => option.Culture.Name == "en-GB").IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void when_current_culture_is_en_gb_then_other_options_are_not_selected()
    {
        var enGb = CultureInfo.GetCultureInfo("en-GB");
        var enUs = CultureInfo.GetCultureInfo("en-US");
        var loc = BuildLocWithCultures(enGb, enGb, enUs);

        var options = LanguageOptionFactory.Create(loc);

        options.Where(option => option.Culture.Name != "en-GB").ShouldAllBe(option => !option.IsSelected);
    }

    [Fact]
    public void when_current_culture_is_en_us_then_en_us_option_is_selected()
    {
        var enGb = CultureInfo.GetCultureInfo("en-GB");
        var enUs = CultureInfo.GetCultureInfo("en-US");
        var loc = BuildLocWithCultures(enUs, enGb, enUs);

        var options = LanguageOptionFactory.Create(loc);

        options.Single(option => option.Culture.Name == "en-US").IsSelected.ShouldBeTrue();
    }
}
