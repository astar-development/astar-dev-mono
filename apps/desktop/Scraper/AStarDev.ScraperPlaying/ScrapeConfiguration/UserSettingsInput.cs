using System.Net.Mail;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>The unvalidated user settings as entered in the configuration editor.</summary>
/// <param name="EmailAddress">The entered email address.</param>
/// <param name="Username">The entered username.</param>
/// <param name="ApiKey">The entered API key.</param>
public sealed record UserSettingsInput(string EmailAddress, string Username, string ApiKey)
{
    /// <summary>Creates the editor input from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static UserSettingsInput From(ScrapeConfigurationEntity entity)
    {
        var settings = UserSettings.From(entity);

        return new UserSettingsInput(settings.EmailAddress, settings.Username, settings.ApiKey);
    }

    /// <summary>Validates the input: the email address may be blank, otherwise it must be a single well-formed address.</summary>
    /// <returns>The validated <see cref="UserSettings"/>, or every validation error found.</returns>
    public Validation<UserSettings> Validate()
    {
        var emailAddress = EmailAddress.Trim();

        return emailAddress.Length == 0 || IsWellFormed(emailAddress)
            ? Validation.Valid(new UserSettings(emailAddress, Username, ApiKey))
            : Validation.Invalid<UserSettings>(ValidationErrorFactory.Create(nameof(EmailAddress), "Must be a well-formed email address."));
    }

    private static bool IsWellFormed(string emailAddress) =>
        MailAddress.TryCreate(emailAddress, out var parsed) && parsed.Address == emailAddress;
}
