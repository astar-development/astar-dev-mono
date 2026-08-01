using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Wallpaper.Scrapper.Models;

/// <summary>
///     Validates a <see cref="ScrapeConfiguration" /> at startup, accumulating every problem so the UI can
///     surface all configuration errors at once instead of the scrape failing part-way through.
/// </summary>
public static class ScrapeConfigurationValidator
{
    /// <summary>
    ///     Validates the supplied <paramref name="configuration" />, accumulating all errors.
    /// </summary>
    /// <param name="configuration">The configuration loaded at startup.</param>
    /// <returns>A <see cref="Valid{T}" /> carrying the configuration, or an <see cref="Invalid{T}" /> carrying every error found.</returns>
    public static Validation<ScrapeConfiguration> Validate(ScrapeConfiguration configuration)
    {
        var errors = new List<ValidationError>();
        errors.AddRange(ValidateSearchConfiguration(configuration.SearchConfiguration));
        errors.AddRange(ValidateUserConfiguration(configuration.UserConfiguration));
        errors.AddRange(ValidateScrapeDirectories(configuration.ScrapeDirectories));

        return errors.Count > 0 ? Validation.Invalid<ScrapeConfiguration>(errors) : Validation.Valid(configuration);
    }

    private static IEnumerable<ValidationError> ValidateSearchConfiguration(SearchConfiguration? searchConfiguration)
    {
        if (searchConfiguration is null)
        {
            yield return ValidationErrorFactory.Create(nameof(ScrapeConfiguration.SearchConfiguration), "The search configuration section is missing.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(searchConfiguration.BaseUrl))
            yield return ValidationErrorFactory.Create($"{nameof(ScrapeConfiguration.SearchConfiguration)}.{nameof(SearchConfiguration.BaseUrl)}", "The base URL is required.");

        if (string.IsNullOrWhiteSpace(searchConfiguration.LoginUrl))
            yield return ValidationErrorFactory.Create($"{nameof(ScrapeConfiguration.SearchConfiguration)}.{nameof(SearchConfiguration.LoginUrl)}", "The login URL is required.");

        if (searchConfiguration.ImagePauseInSeconds < 0)
            yield return ValidationErrorFactory.Create($"{nameof(ScrapeConfiguration.SearchConfiguration)}.{nameof(SearchConfiguration.ImagePauseInSeconds)}", "The image pause cannot be negative.");

        if (searchConfiguration.StartingPageNumber < 1)
            yield return ValidationErrorFactory.Create($"{nameof(ScrapeConfiguration.SearchConfiguration)}.{nameof(SearchConfiguration.StartingPageNumber)}", "The starting page number must be at least 1.");
    }

    private static IEnumerable<ValidationError> ValidateUserConfiguration(UserConfiguration? userConfiguration)
    {
        if (userConfiguration is null)
        {
            yield return ValidationErrorFactory.Create(nameof(ScrapeConfiguration.UserConfiguration), "The user configuration section is missing.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(userConfiguration.LoginEmailAddress))
            yield return ValidationErrorFactory.Create($"{nameof(ScrapeConfiguration.UserConfiguration)}.{nameof(UserConfiguration.LoginEmailAddress)}", "The login email address is required.");

        if (string.IsNullOrWhiteSpace(userConfiguration.Password))
            yield return ValidationErrorFactory.Create($"{nameof(ScrapeConfiguration.UserConfiguration)}.{nameof(UserConfiguration.Password)}", "The password is required.");
    }

    private static IEnumerable<ValidationError> ValidateScrapeDirectories(ScrapeDirectories? scrapeDirectories)
    {
        if (scrapeDirectories is null)
        {
            yield return ValidationErrorFactory.Create(nameof(ScrapeConfiguration.ScrapeDirectories), "The scrape directories section is missing.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(scrapeDirectories.RootDirectory))
            yield return ValidationErrorFactory.Create($"{nameof(ScrapeConfiguration.ScrapeDirectories)}.{nameof(ScrapeDirectories.RootDirectory)}", "The root directory is required.");

        if (string.IsNullOrWhiteSpace(scrapeDirectories.BaseSaveDirectory))
            yield return ValidationErrorFactory.Create($"{nameof(ScrapeConfiguration.ScrapeDirectories)}.{nameof(ScrapeDirectories.BaseSaveDirectory)}", "The base save directory is required.");
    }
}
