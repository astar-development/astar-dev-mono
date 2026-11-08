namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>The Entra app registration used to sign in to personal Microsoft accounts.</summary>
public sealed class MicrosoftAccountOptions
{
    /// <summary>The configuration section holding these options.</summary>
    public const string SectionName = "MicrosoftAccount";

    /// <summary>The application (client) id of the app registration.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>The sign-in authority; <c>consumers</c> limits sign-in to personal Microsoft accounts.</summary>
    public Uri Authority { get; init; } = new("https://login.microsoftonline.com/consumers");

    /// <summary>The loopback redirect the system browser returns to.</summary>
    public Uri RedirectUri { get; init; } = new("http://localhost");

    /// <summary>The Microsoft Graph permissions requested at sign-in.</summary>
    public string[] Scopes { get; init; } = [];
}
