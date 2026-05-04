namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.OneDrive;

/// <summary>Factory for <see cref="OneDriveClientOptions"/>.</summary>
public static class OneDriveClientOptionsFactory
{
    /// <summary>Creates a new <see cref="OneDriveClientOptions"/> instance.</summary>
    public static OneDriveClientOptions Create(string azureClientId, Uri redirectUri)
        => new() { AzureClientId = azureClientId, RedirectUri = redirectUri };
}
