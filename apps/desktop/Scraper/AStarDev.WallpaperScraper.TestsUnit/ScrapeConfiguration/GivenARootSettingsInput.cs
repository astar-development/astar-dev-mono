using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenARootSettingsInput
{
    private static readonly RootSettingsInput ValidInput = new(
        new WallhavenUrlsInput("https://wallhaven.cc/api/v1", "https://wallhaven.cc/login", "top", "hot", "subscriptions"),
        "api-key",
        "cats",
        "prefix-",
        "-suffix",
        5,
        new PageRanges(new PageRange(1, 20), new PageRange(2, 30), new PageRange(3, 40), new PageRange(4, 50)),
        new BrowserOptions(true, Option.Some(250f)));

    [Fact]
    public void when_the_input_is_valid_then_the_settings_carry_every_value()
    {
        var result = ValidInput.Validate();

        var expected = new RootSettings(
            new WallhavenUrls(new Uri("https://wallhaven.cc/api/v1"), "https://wallhaven.cc/login", "top", "hot", "subscriptions"),
            "api-key",
            "cats",
            "prefix-",
            "-suffix",
            5,
            new PageRanges(new PageRange(1, 20), new PageRange(2, 30), new PageRange(3, 40), new PageRange(4, 50)),
            new BrowserOptions(true, Option.Some(250f)));
        result.ShouldBeOfType<Valid<RootSettings>>().Value.ShouldBe(expected);
    }

    [Fact]
    public void when_the_settings_are_converted_to_input_then_validating_returns_the_same_settings()
    {
        var settings = ValidInput.Validate().ShouldBeOfType<Valid<RootSettings>>().Value;

        settings.ToInput().ShouldBe(ValidInput);
        settings.ToInput().Validate().ShouldBeOfType<Valid<RootSettings>>().Value.ShouldBe(settings);
    }

    [Fact]
    public void when_the_starting_page_equals_the_total_pages_then_the_input_is_valid() =>
        (ValidInput with { Pages = ValidInput.Pages with { Search = new PageRange(20, 20) } }).Validate().ShouldBeOfType<Valid<RootSettings>>();

    [Fact]
    public void when_the_slow_motion_delay_is_absent_then_the_input_is_valid() =>
        (ValidInput with { Browser = new BrowserOptions(true, Option.None<float>()) }).Validate().ShouldBeOfType<Valid<RootSettings>>();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("relative/path")]
    [InlineData("ftp://example.com")]
    public void when_the_base_url_is_not_an_absolute_http_url_then_it_is_rejected(string enteredText) =>
        ErrorsFor(WithUrls(ValidInput.Urls with { BaseUrl = enteredText })).ShouldContain(error => error.Property == "BaseUrl");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("login")]
    [InlineData("not a url, just text: /login?x=1 & more")]
    [InlineData("ftp://example.com/login")]
    public void when_the_login_url_is_any_text_then_it_is_valid_and_stored_exactly_as_entered(string enteredText) =>
        WithUrls(ValidInput.Urls with { LoginUrl = enteredText }).Validate().ShouldBeOfType<Valid<RootSettings>>().Value.Urls.LoginUrl.ShouldBe(enteredText);

    [Fact]
    public void when_the_image_pause_is_negative_then_it_is_rejected() =>
        ErrorsFor(ValidInput with { ImagePauseInSeconds = -1 }).ShouldContain(error => error.Property == nameof(RootSettingsInput.ImagePauseInSeconds));

    [Fact]
    public void when_the_image_pause_is_zero_then_the_input_is_valid() =>
        (ValidInput with { ImagePauseInSeconds = 0 }).Validate().ShouldBeOfType<Valid<RootSettings>>();

    [Fact]
    public void when_a_page_count_is_negative_then_it_is_rejected()
    {
        ErrorsFor(WithPages(ValidInput.Pages with { Search = new PageRange(-1, 20) })).ShouldContain(error => error.Property == "StartingPageNumber");
        ErrorsFor(WithPages(ValidInput.Pages with { Search = new PageRange(1, -1) })).ShouldContain(error => error.Property == "TotalPages");
        ErrorsFor(WithPages(ValidInput.Pages with { Subscriptions = new PageRange(-1, 30) })).ShouldContain(error => error.Property == "SubscriptionsStartingPageNumber");
        ErrorsFor(WithPages(ValidInput.Pages with { Subscriptions = new PageRange(2, -1) })).ShouldContain(error => error.Property == "SubscriptionsTotalPages");
        ErrorsFor(WithPages(ValidInput.Pages with { TopWallpapers = new PageRange(-1, 40) })).ShouldContain(error => error.Property == "TopWallpapersStartingPageNumber");
        ErrorsFor(WithPages(ValidInput.Pages with { TopWallpapers = new PageRange(3, -1) })).ShouldContain(error => error.Property == "TopWallpapersTotalPages");
        ErrorsFor(WithPages(ValidInput.Pages with { HotWallpapers = new PageRange(-1, 50) })).ShouldContain(error => error.Property == "HotWallpapersStartingPageNumber");
        ErrorsFor(WithPages(ValidInput.Pages with { HotWallpapers = new PageRange(4, -1) })).ShouldContain(error => error.Property == "HotWallpapersTotalPages");
    }

    [Fact]
    public void when_a_starting_page_exceeds_its_total_pages_then_it_is_rejected()
    {
        ErrorsFor(WithPages(ValidInput.Pages with { Search = new PageRange(21, 20) })).ShouldContain(error => error.Property == "StartingPageNumber");
        ErrorsFor(WithPages(ValidInput.Pages with { Subscriptions = new PageRange(31, 30) })).ShouldContain(error => error.Property == "SubscriptionsStartingPageNumber");
        ErrorsFor(WithPages(ValidInput.Pages with { TopWallpapers = new PageRange(41, 40) })).ShouldContain(error => error.Property == "TopWallpapersStartingPageNumber");
        ErrorsFor(WithPages(ValidInput.Pages with { HotWallpapers = new PageRange(51, 50) })).ShouldContain(error => error.Property == "HotWallpapersStartingPageNumber");
    }

    [Fact]
    public void when_the_slow_motion_delay_is_negative_then_it_is_rejected() =>
        ErrorsFor(ValidInput with { Browser = new BrowserOptions(true, Option.Some(-1f)) }).ShouldContain(error => error.Property == nameof(BrowserOptions.SlowMotionDelay));

    [Fact]
    public void when_several_values_are_invalid_then_every_error_is_reported() =>
        ErrorsFor(ValidInput with { Urls = ValidInput.Urls with { BaseUrl = string.Empty }, ImagePauseInSeconds = -1 }).Count.ShouldBe(2);

    private static RootSettingsInput WithUrls(WallhavenUrlsInput urls) => ValidInput with { Urls = urls };

    private static RootSettingsInput WithPages(PageRanges pages) => ValidInput with { Pages = pages };

    private static IReadOnlyList<ValidationError> ErrorsFor(RootSettingsInput input) =>
        input.Validate().ShouldBeOfType<Invalid<RootSettings>>().Errors;
}
