namespace Fab4Kids.Web.Newsletter;

/// <summary>
/// Newsletter subscriber-storage and confirmation-email settings, bound from the
/// <c>Newsletter</c> configuration section (Azure App Service application settings in production).
/// </summary>
public sealed class NewsletterOptions
{
    /// <summary>Azure Table Storage connection string. Absent until the storage account is provisioned.</summary>
    public string? TableStorageConnectionString { get; init; }

    /// <summary>The table that holds subscriber records.</summary>
    public string? TableName { get; init; }

    /// <summary>Azure Communication Services connection string used to send the confirmation email.</summary>
    public string? EmailConnectionString { get; init; }

    /// <summary>The verified ACS sender address, e.g. <c>DoNotReply@&lt;verified-domain&gt;</c>.</summary>
    public string? FromAddress { get; init; }
}
