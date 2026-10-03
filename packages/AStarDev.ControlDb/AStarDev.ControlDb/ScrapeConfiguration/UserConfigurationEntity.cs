namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>Represents a user configuration entity in the database.</summary>
/// <param name="Id">The unique identifier for the user configuration entity.</param>
/// <param name="ScrapeConfigurationEntityId">The unique identifier for the associated scrape configuration entity.</param>
/// <param name="EmailAddress">The email address of the user.</param>
/// <param name="Username">The username of the user.</param>
/// <param name="ApiKey">The API key of the user.</param>
public record UserConfigurationEntity(UserConfigurationId Id, ScrapeConfigurationId ScrapeConfigurationEntityId, string EmailAddress, string Username, string ApiKey)
{
    /// <summary>The email address of the user.</summary>
    public string EmailAddress { get; set; } = EmailAddress;

    /// <summary>The username of the user.</summary>
    public string Username { get; set; } = Username;

    /// <summary>The API key of the user.</summary>
    public string ApiKey { get; set; } = ApiKey;
}
