namespace AStar.Dev.OneDrive.Client;

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
