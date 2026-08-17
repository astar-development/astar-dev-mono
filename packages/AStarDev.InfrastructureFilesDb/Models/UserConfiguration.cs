namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
/// Represents user configuration details, including login email address, username, password, and session cookie. This information is essential for authenticating the scraper with the target website, allowing it to access user-specific content and perform actions that require authentication. The LoginEmailAddress and Username are used for identifying the user account, while the Password is necessary for logging in. The SessionCookie can be used to maintain an authenticated session without needing to log in repeatedly, improving the efficiency of the scraping process and reducing the likelihood of triggering anti-bot measures on the target website.
/// </summary>
public class UserConfiguration : AuditableEntity
{
    /// <summary>
    /// The internal primary key for the UserConfiguration table.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The foreign key back to the parent scrape configuration.
    /// </summary>
    public int ScrapeConfigurationEntityId { get; set; }

    /// <summary>
    /// Navigation property to the parent scrape configuration.
    /// </summary>
    public ScrapeConfigurationEntity? ScrapeConfigurationEntity { get; set; }

    /// <summary>
    /// The login email address is the email associated with the user's account on the target website. It is used as part of the authentication process when logging in to the website. The scraper will use this email address, along with the password, to authenticate and gain access to user-specific content and features on the website. Providing a valid login email address is crucial for successful authentication and ensuring that the scraper can access the necessary data for scraping.
    /// </summary>
    public string LoginEmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// The username is the unique identifier for the user's account on the target website. It is used in conjunction with the password to authenticate the user and gain access to their account. The scraper will use the username during the login process to identify which account it is trying to access. Providing a valid username is essential for successful authentication and ensuring that the scraper can access the necessary data for scraping.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password is the secret key associated with the user's account on the target website. It is used in conjunction with the username to authenticate the user and gain access to their account. The scraper will use the password during the login process to verify the user's identity and ensure that it has permission to access the account. Providing a valid password is crucial for successful authentication and ensuring that the scraper can access the necessary data for scraping. It is important to handle the password securely and avoid exposing it in logs or code repositories to prevent unauthorized access to the user's account.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The session cookie is a piece of data that is stored in the user's browser after a successful login to the target website. It is used to maintain an authenticated session without needing to log in repeatedly. The scraper can use the session cookie to access user-specific content and perform actions that require authentication without having to go through the login process each time. This can improve the efficiency of the scraping process and reduce the likelihood of triggering anti-bot measures on the target website. Providing a valid session cookie allows the scraper to maintain an authenticated session and access the necessary data for scraping without needing to log in repeatedly.
    /// </summary>
    public string SessionCookie { get; set; } = string.Empty;
}

