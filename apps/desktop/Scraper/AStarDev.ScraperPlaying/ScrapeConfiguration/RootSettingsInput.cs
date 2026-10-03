using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>The unvalidated root-level settings as entered in the configuration editor.</summary>
public sealed record RootSettingsInput(
    WallhavenUrlsInput Urls,
    string ApiKey,
    string SearchString,
    string SearchStringPrefix,
    string SearchStringSuffix,
    int ImagePauseInSeconds,
    PageRanges Pages,
    BrowserOptions Browser)
{
    /// <summary>Creates the editor input from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static RootSettingsInput From(ScrapeConfigurationEntity entity) => RootSettings.From(entity).ToInput();

    /// <summary>Validates the input: the base URL must be absolute http(s), the login URL is free text, numbers must not be negative and a starting page must not exceed its total pages.</summary>
    /// <returns>The validated <see cref="RootSettings"/>, or every validation error found.</returns>
    public Validation<RootSettings> Validate()
    {
        List<ValidationError> errors = [];
        var baseUrl = ParseUrl("BaseUrl", Urls.BaseUrl, errors);
        _ = RequireNotNegative(nameof(ImagePauseInSeconds), ImagePauseInSeconds, errors);
        Pages.Validate(errors);
        Browser.Validate(errors);

        return errors.Count > 0
            ? Validation.Invalid<RootSettings>(errors)
            : baseUrl.Match(url => Validation.Valid(ToSettings(url)), () => Validation.Invalid<RootSettings>(errors));
    }

    internal static bool RequireNotNegative(string property, float value, List<ValidationError> errors)
    {
        if (value >= 0) return true;

        errors.Add(ValidationErrorFactory.Create(property, "Must not be negative."));

        return false;
    }

    private static Option<Uri> ParseUrl(string property, string text, List<ValidationError> errors)
    {
        if (Uri.TryCreate(text, UriKind.Absolute, out var url) && url.Scheme is "http" or "https") return Option.Some(url);

        errors.Add(ValidationErrorFactory.Create(property, "Must be an absolute http or https URL."));

        return Option.None<Uri>();
    }

    private RootSettings ToSettings(Uri baseUrl) => new(
        new WallhavenUrls(baseUrl, Urls.LoginUrl, Urls.TopWallpapers, Urls.HotWallpapers, Urls.Subscriptions),
        ApiKey,
        SearchString,
        SearchStringPrefix,
        SearchStringSuffix,
        ImagePauseInSeconds,
        Pages,
        Browser);
}
