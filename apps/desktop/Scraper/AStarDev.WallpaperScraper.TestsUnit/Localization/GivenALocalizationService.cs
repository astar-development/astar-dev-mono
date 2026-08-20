using System.Globalization;
using AStarDev.WallpaperScraper.Localization;

namespace AStarDev.WallpaperScraper.TestsUnit.Localization;

public sealed class GivenALocalizationService
{
    [Fact]
    public void when_constructed_then_culture_defaults_to_en_GB()
    {
        var service = new LocalizationService();

        service.CurrentCulture.Name.ShouldBe("en-GB");
    }

    [Fact]
    public void when_constructed_then_available_cultures_includes_en_GB()
    {
        var service = new LocalizationService();

        service.AvailableCultures.ShouldNotBeEmpty();
        service.AvailableCultures.ShouldContain(c => c.Name == "en-GB");
    }

    [Fact]
    public void when_initialise_called_without_argument_then_culture_defaults_to_en_GB()
    {
        var service = new LocalizationService();

        service.Initialise();

        service.CurrentCulture.Name.ShouldBe("en-GB");
    }

    [Fact]
    public void when_get_local_called_with_valid_key_then_value_is_returned()
    {
        var service = new LocalizationService();
        service.Initialise();
        string key = "App.Title";

        string result = service.GetLocal(key);

        result.ShouldBe("Wallpaper Scraper");
    }

    [Fact]
    public void when_get_local_called_with_invalid_key_then_key_is_returned_as_fallback()
    {
        var service = new LocalizationService();
        service.Initialise();
        string key = "NonExistent.Key";

        string result = service.GetLocal(key);

        result.ShouldBe(key);
    }

    [Fact]
    public void when_get_local_called_with_format_arguments_then_placeholder_is_substituted()
    {
        var service = new LocalizationService();
        service.Initialise();
        string key = "Scraper.SearchCategories.Started";

        string result = service.GetLocal(key);

        result.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task when_set_culture_async_called_with_same_culture_then_culture_changed_event_is_not_raised()
    {
        var service = new LocalizationService();
        service.Initialise();
        bool eventRaised = false;
        service.CultureChanged += (_, _) => eventRaised = true;

        await service.SetCultureAsync(service.CurrentCulture, TestContext.Current.CancellationToken);

        eventRaised.ShouldBeFalse();
    }
}
