namespace AStar.Dev.OneDrive.Client;

/// <summary>
///     Configuration options for the OneDrive Client package.
/// </summary>
public sealed record OneDriveClientOptions
{
    /// <summary>The Azure App Registration client ID for the consumers tenant.</summary>
    public required string AzureClientId { get; init; }

    /// <summary>Redirect URI registered in the Azure portal (local loopback or <c>urn:ietf:wg:oauth:2.0:oob</c>).</summary>
    public required Uri RedirectUri { get; init; }
}

/// <summary>Factory for <see cref="OneDriveClientOptions"/>.</summary>
public static class OneDriveClientOptionsFactory
{
    /// <summary>Creates a new <see cref="OneDriveClientOptions"/> instance.</summary>
    public static OneDriveClientOptions Create(string azureClientId, Uri redirectUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(azureClientId);
        ArgumentNullException.ThrowIfNull(redirectUri);

        return new OneDriveClientOptions { AzureClientId = azureClientId, RedirectUri = redirectUri };
    }
}
