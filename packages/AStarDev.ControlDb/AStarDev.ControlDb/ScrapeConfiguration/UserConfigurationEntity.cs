namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>
/// Represents a user configuration entity in the database.
/// </summary>
/// <param name="Id">The unique identifier for the user configuration entity.</param>
/// <param name="ScrapeConfigurationEntityId">The unique identifier for the associated scrape configuration entity.</param>
/// <param name="EmailAddress">The email address of the user.</param>
/// <param name="Username">The username of the user.</param>
/// <param name="Password">The password of the user.</param>
/// <param name="SessionCookie">The session cookie of the user.</param>
public record UserConfigurationEntity(UserConfigurationId Id, ScrapeConfigurationId ScrapeConfigurationEntityId, string EmailAddress, string Username, string Password, string SessionCookie);
