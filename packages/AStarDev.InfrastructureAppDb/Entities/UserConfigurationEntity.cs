namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>The user authentication details the scraper uses to access the target website.</summary>
public sealed class UserConfigurationEntity : AuditableEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the parent scrape configuration.</summary>
    public int ScrapeConfigurationEntityId { get; set; }

    /// <summary>Navigation property to the parent scrape configuration.</summary>
    public ScrapeConfigurationEntity? ScrapeConfigurationEntity { get; set; }

    /// <summary>The login email address for the target website.</summary>
    public string LoginEmailAddress { get; set; } = string.Empty;

    /// <summary>The username for the target website.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>The password for the target website.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>The session cookie used to maintain an authenticated session.</summary>
    public string SessionCookie { get; set; } = string.Empty;
}
