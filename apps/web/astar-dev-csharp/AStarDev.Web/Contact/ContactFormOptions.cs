namespace AStarDev.Web.Contact;

/// <summary>
/// Contact-form email settings, bound from the <c>ContactForm</c> configuration section
/// (Azure App Service application settings in production).
/// </summary>
public sealed class ContactFormOptions
{
    /// <summary>Azure Communication Services connection string. Absent until the ACS resource is provisioned (see #848).</summary>
    public string? ConnectionString { get; init; }

    /// <summary>The verified ACS sender address, e.g. <c>DoNotReply@&lt;verified-domain&gt;</c>.</summary>
    public string? FromAddress { get; init; }

    /// <summary>The mailbox that receives contact-form submissions.</summary>
    public string? ToAddress { get; init; }
}
